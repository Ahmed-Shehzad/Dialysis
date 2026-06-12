using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Features.AssignRoleToUser;
using Dialysis.Identity.Provisioning.Features.DeactivateUser;
using Dialysis.Identity.Provisioning.Features.DefineRole;
using Dialysis.Identity.Provisioning.Features.ListUserPermissions;
using Dialysis.Identity.Provisioning.Features.ProvisionUser;
using Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;
using Shouldly;
using Xunit;

namespace Dialysis.Identity.Tests.Provisioning;

/// <summary>
/// Locks down the business rules of the user/role provisioning handlers: idempotency on natural
/// keys (subject, role code, existing assignment), the deactivated-user gate, the permission
/// snapshot carried by <see cref="RoleAssignedIntegrationEvent"/>, and the union/dedupe semantics
/// of permission resolution. The outbox hand-off itself is covered by
/// <see cref="IdentityProvisioningOutboxTests"/>; these tests pin the decisions made before it.
/// </summary>
public sealed class ProvisioningHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUserAccountRepository _users = new();
    private readonly InMemoryRoleDefinitionRepository _roles = new();
    private readonly InMemoryRoleAssignmentRepository _assignments = new();
    private readonly RecordingTransponderOutbox _outbox = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly FixedClock _clock = new(_now);

    [Fact]
    public async Task Provision_User_Creates_A_Provisioned_Account_And_Enqueues_User_Registered_Async()
    {
        var handler = new ProvisionUserCommandHandler(_users, _outbox, new JsonTestMessageSerializer(), _unitOfWork, _clock);

        var id = await handler.HandleAsync(
            new ProvisionUserCommand("keycloak|abc", "Dr. Grey", "grey@example.com"),
            CancellationToken.None);

        var user = _users.All.ShouldHaveSingleItem();
        user.Id.ShouldBe(id);
        user.Status.ShouldBe(UserAccountStatus.Provisioned);

        var envelope = _outbox.Enqueued.ShouldHaveSingleItem();
        envelope.AssemblyQualifiedEventType.ShouldStartWith(typeof(UserRegisteredIntegrationEvent).FullName!);
        envelope.PayloadJson.ShouldContain("keycloak|abc");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Provision_User_Is_Idempotent_On_Subject_Async()
    {
        var existing = SeedUser("keycloak|abc");
        var handler = new ProvisionUserCommandHandler(_users, _outbox, new JsonTestMessageSerializer(), _unitOfWork, _clock);

        var id = await handler.HandleAsync(
            new ProvisionUserCommand("keycloak|abc", "Different Name", null),
            CancellationToken.None);

        id.ShouldBe(existing.Id);
        _users.All.ShouldHaveSingleItem();
        _outbox.Enqueued.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Define_Role_Is_Idempotent_On_Code_Async()
    {
        var existing = SeedRole("his.facility-admin", "his.staff.assign");
        var handler = new DefineRoleCommandHandler(_roles, _unitOfWork);

        var id = await handler.HandleAsync(
            new DefineRoleCommand("his.facility-admin", "Renamed", ["other.permission"]),
            CancellationToken.None);

        id.ShouldBe(existing.Id);
        var role = _roles.All.ShouldHaveSingleItem();
        role.Permissions.ShouldBe(["his.staff.assign"]);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Assign_Role_Throws_When_The_User_Is_Unknown_Async()
    {
        SeedRole("his.nurse");
        var handler = BuildAssignHandler();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new AssignRoleToUserCommand(Guid.NewGuid(), "his.nurse"), CancellationToken.None));

        ex.Message.ShouldContain("not found");
        _assignments.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Assign_Role_Rejects_A_Deactivated_User_Async()
    {
        var user = SeedUser("keycloak|abc", UserAccountStatus.Deactivated);
        SeedRole("his.nurse");
        var handler = BuildAssignHandler();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new AssignRoleToUserCommand(user.Id, "his.nurse"), CancellationToken.None));

        ex.Message.ShouldContain("deactivated");
        _assignments.All.ShouldBeEmpty();
        _outbox.Enqueued.ShouldBeEmpty();
    }

    [Fact]
    public async Task Assign_Role_Throws_When_The_Role_Is_Not_Defined_Async()
    {
        var user = SeedUser("keycloak|abc");
        var handler = BuildAssignHandler();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new AssignRoleToUserCommand(user.Id, "ghost.role"), CancellationToken.None));

        ex.Message.ShouldContain("ghost.role");
        _assignments.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Assign_Role_Is_Idempotent_When_The_Assignment_Already_Exists_Async()
    {
        var user = SeedUser("keycloak|abc");
        var role = SeedRole("his.nurse");
        _assignments.Add(new RoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, AssignedAtUtc = _now.UtcDateTime });
        var handler = BuildAssignHandler();

        await handler.HandleAsync(new AssignRoleToUserCommand(user.Id, "his.nurse"), CancellationToken.None);

        _assignments.All.ShouldHaveSingleItem();
        _outbox.Enqueued.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Assign_Role_Records_The_Assignment_And_Snapshots_Permissions_Into_The_Event_Async()
    {
        var user = SeedUser("keycloak|abc");
        SeedRole("his.facility-admin", "his.staff.assign", "his.inventory.move");
        var handler = BuildAssignHandler();

        await handler.HandleAsync(new AssignRoleToUserCommand(user.Id, "his.facility-admin"), CancellationToken.None);

        var assignment = _assignments.All.ShouldHaveSingleItem();
        assignment.UserId.ShouldBe(user.Id);
        assignment.AssignedAtUtc.ShouldBe(_now.UtcDateTime);

        var envelope = _outbox.Enqueued.ShouldHaveSingleItem();
        envelope.AssemblyQualifiedEventType.ShouldStartWith(typeof(RoleAssignedIntegrationEvent).FullName!);
        envelope.PayloadJson.ShouldContain("his.staff.assign");
        envelope.PayloadJson.ShouldContain("his.inventory.move");
        envelope.PayloadJson.ShouldContain("keycloak|abc");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Revoke_Role_Is_A_Noop_When_The_Role_Was_Never_Assigned_Async()
    {
        var user = SeedUser("keycloak|abc");
        SeedRole("his.nurse");
        var handler = BuildRevokeHandler();

        await handler.HandleAsync(new RevokeRoleFromUserCommand(user.Id, "his.nurse"), CancellationToken.None);

        _outbox.Enqueued.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Revoke_Role_Removes_The_Assignment_And_Publishes_Role_Revoked_Async()
    {
        var user = SeedUser("keycloak|abc");
        var role = SeedRole("his.nurse");
        _assignments.Add(new RoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, AssignedAtUtc = _now.UtcDateTime });
        var handler = BuildRevokeHandler();

        await handler.HandleAsync(new RevokeRoleFromUserCommand(user.Id, "his.nurse"), CancellationToken.None);

        _assignments.All.ShouldBeEmpty();
        var revoked = _outbox.Enqueued.ShouldHaveSingleItem();
        revoked.AssemblyQualifiedEventType.ShouldStartWith(typeof(RoleRevokedIntegrationEvent).FullName!);
        revoked.PayloadJson.ShouldContain(user.Id.ToString());
        revoked.PayloadJson.ShouldContain("keycloak|abc");
        revoked.PayloadJson.ShouldContain("his.nurse");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Deactivate_User_Flips_Status_And_Publishes_User_Deactivated_Async()
    {
        var user = SeedUser("keycloak|abc");
        var handler = new DeactivateUserCommandHandler(_users, _outbox, new JsonTestMessageSerializer(), _unitOfWork, _clock);

        await handler.HandleAsync(new DeactivateUserCommand(user.Id), CancellationToken.None);

        _users.All.ShouldHaveSingleItem().Status.ShouldBe(UserAccountStatus.Deactivated);
        var deactivated = _outbox.Enqueued.ShouldHaveSingleItem();
        deactivated.AssemblyQualifiedEventType.ShouldStartWith(typeof(UserDeactivatedIntegrationEvent).FullName!);
        deactivated.PayloadJson.ShouldContain(user.Id.ToString());
        deactivated.PayloadJson.ShouldContain("keycloak|abc");
    }

    [Fact]
    public async Task Deactivate_User_Is_Idempotent_When_Already_Deactivated_Async()
    {
        var user = SeedUser("keycloak|abc", UserAccountStatus.Deactivated);
        var handler = new DeactivateUserCommandHandler(_users, _outbox, new JsonTestMessageSerializer(), _unitOfWork, _clock);

        await handler.HandleAsync(new DeactivateUserCommand(user.Id), CancellationToken.None);

        _outbox.Enqueued.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task List_User_Permissions_Returns_Null_For_An_Unknown_User_Async()
    {
        var handler = new ListUserPermissionsQueryHandler(_users, _roles, _assignments);

        var dto = await handler.HandleAsync(new ListUserPermissionsQuery(Guid.NewGuid()), CancellationToken.None);

        dto.ShouldBeNull();
    }

    [Fact]
    public async Task List_User_Permissions_Unions_Deduplicates_And_Sorts_Across_Roles_Async()
    {
        var user = SeedUser("keycloak|abc");
        var nurse = SeedRole("his.nurse", "his.chart.read", "his.queue.read");
        var admin = SeedRole("his.facility-admin", "his.queue.read", "his.staff.assign");
        _assignments.Add(new RoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = nurse.Id, AssignedAtUtc = _now.UtcDateTime });
        _assignments.Add(new RoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = admin.Id, AssignedAtUtc = _now.UtcDateTime });
        var handler = new ListUserPermissionsQueryHandler(_users, _roles, _assignments);

        var dto = await handler.HandleAsync(new ListUserPermissionsQuery(user.Id), CancellationToken.None);

        dto.ShouldNotBeNull();
        dto.Subject.ShouldBe("keycloak|abc");
        dto.Roles.ShouldBe(["his.nurse", "his.facility-admin"], ignoreOrder: true);
        dto.Permissions.ShouldBe(["his.chart.read", "his.queue.read", "his.staff.assign"]);
    }

    private AssignRoleToUserCommandHandler BuildAssignHandler() =>
        new(_users, _roles, _assignments, _outbox, new JsonTestMessageSerializer(), _unitOfWork, _clock);

    private RevokeRoleFromUserCommandHandler BuildRevokeHandler() =>
        new(_users, _roles, _assignments, _outbox, new JsonTestMessageSerializer(), _unitOfWork, _clock);

    private UserAccount SeedUser(string subject, UserAccountStatus status = UserAccountStatus.Provisioned)
    {
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            DisplayName = "Seeded User",
            Status = status,
        };
        _users.Add(user);
        return user;
    }

    private RoleDefinition SeedRole(string code, params string[] permissions)
    {
        var role = new RoleDefinition
        {
            Id = Guid.NewGuid(),
            Code = code,
            DisplayName = code,
            Permissions = [.. permissions],
        };
        _roles.Add(role);
        return role;
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _fixedNow;
        public FixedClock(DateTimeOffset now) => _fixedNow = now;
        public override DateTimeOffset GetUtcNow() => _fixedNow;
    }
}
