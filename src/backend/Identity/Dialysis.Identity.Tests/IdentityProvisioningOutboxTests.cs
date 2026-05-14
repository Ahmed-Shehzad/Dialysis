using Dialysis.CQRS;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Persistence;
using Dialysis.Identity.Provisioning.Features.AssignRoleToUser;
using Dialysis.Identity.Provisioning.Features.DefineRole;
using Dialysis.Identity.Provisioning.Features.ProvisionUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.Identity.Tests;

/// <summary>
/// Smoke-test the cross-module publish contract: when the Identity module mutates its aggregates
/// the resulting <c>IIntegrationEvent</c> lands in the Transponder transactional outbox so the
/// outbox relay → RabbitMQ leg can ship it to other modules. RabbitMQ itself is exercised by the
/// Transponder building block's own tests; here we just lock down the per-module hand-off.
/// </summary>
[Collection(nameof(IdentityFixtureCollection))]
public sealed class IdentityProvisioningOutboxTests(IdentityApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Provisioning_a_user_writes_a_UserRegistered_row_into_the_outbox()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var subject = $"keycloak|{Guid.CreateVersion7()}";
        await gateway.SendCommandAsync<ProvisionUserCommand, Guid>(
            new ProvisionUserCommand(subject, "Alice Tester", "alice@example.com"),
            CancellationToken.None);

        var rows = await db.OutboxMessages
            .AsNoTracking()
            .OrderBy(o => o.CreatedAtUtc)
            .ToListAsync();

        rows.ShouldNotBeEmpty();
        rows.ShouldContain(r => r.AssemblyQualifiedEventType.StartsWith(typeof(UserRegisteredIntegrationEvent).FullName!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Assigning_a_role_writes_a_RoleAssigned_row_into_the_outbox()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var subject = $"keycloak|{Guid.CreateVersion7()}";
        var userId = await gateway.SendCommandAsync<ProvisionUserCommand, Guid>(
            new ProvisionUserCommand(subject, "Bob Tester", null),
            CancellationToken.None);

        await gateway.SendCommandAsync<DefineRoleCommand, Guid>(
            new DefineRoleCommand("his.facility-admin", "HIS Facility Admin", ["his.staff.assign", "his.inventory.move"]),
            CancellationToken.None);

        await gateway.SendCommandAsync<AssignRoleToUserCommand, Dialysis.CQRS.Unit>(
            new AssignRoleToUserCommand(userId, "his.facility-admin"),
            CancellationToken.None);

        var rows = await db.OutboxMessages
            .AsNoTracking()
            .Where(o => o.AssemblyQualifiedEventType.StartsWith(typeof(RoleAssignedIntegrationEvent).FullName!))
            .ToListAsync();

        rows.ShouldHaveSingleItem();
        rows[0].PayloadJson.ShouldContain("his.facility-admin");
        rows[0].PayloadJson.ShouldContain("his.staff.assign");
    }
}
