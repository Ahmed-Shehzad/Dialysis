using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.Identity.Contracts.Integration;

namespace Dialysis.Identity.Provisioning.Domain;

public enum UserAccountStatus
{
    Provisioned = 1,
    Deactivated = 2,
}

/// <summary>
/// Identity module's authoritative record of an end user. The <see cref="Subject"/> is the
/// stable identifier issued by the upstream OIDC provider (e.g. Keycloak's <c>sub</c> claim).
/// Lifecycle integration events are raised here and drained to the Transponder outbox by the
/// SaveChanges interceptor — never published manually from handlers.
/// </summary>
public sealed class UserAccount : AggregateRoot<Guid>
{
    private UserAccount()
    {
    }

    private UserAccount(Guid id) : base(id)
    {
    }

    public string Subject { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public UserAccountStatus Status { get; private set; } = UserAccountStatus.Provisioned;

    /// <summary>Provisions a new account for an upstream OIDC subject, raising <see cref="UserRegisteredIntegrationEvent"/>.</summary>
    public static UserAccount Provision(Guid id, string subject, string displayName, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var user = new UserAccount(id)
        {
            Subject = subject,
            DisplayName = displayName,
            Email = email,
            Status = UserAccountStatus.Provisioned,
        };

        user.RaiseIntegrationEvent(new UserRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            UserId: id,
            Subject: subject,
            DisplayName: displayName,
            Email: email));

        return user;
    }

    /// <summary>
    /// Deactivates the account, raising <see cref="UserDeactivatedIntegrationEvent"/>.
    /// Idempotent: an already-deactivated account is a no-op and raises nothing.
    /// </summary>
    public bool Deactivate()
    {
        if (Status == UserAccountStatus.Deactivated)
            return false;

        Status = UserAccountStatus.Deactivated;
        RaiseIntegrationEvent(new UserDeactivatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            UserId: Id,
            Subject: Subject));
        return true;
    }

    /// <summary>
    /// Records that a role was granted to this account, raising
    /// <see cref="RoleAssignedIntegrationEvent"/>. The assignment row itself is persisted by the
    /// caller; the event rides this (tracked) aggregate into the outbox in the same transaction.
    /// </summary>
    public void RecordRoleAssigned(string roleCode, IReadOnlyList<string> permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        ArgumentNullException.ThrowIfNull(permissions);
        if (Status == UserAccountStatus.Deactivated)
            throw new InvalidOperationException($"User '{Id}' is deactivated.");

        RaiseIntegrationEvent(new RoleAssignedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            UserId: Id,
            Subject: Subject,
            RoleCode: roleCode,
            Permissions: [.. permissions]));
    }

    /// <summary>
    /// Records that a role was revoked from this account, raising
    /// <see cref="RoleRevokedIntegrationEvent"/>. The assignment row removal is persisted by the
    /// caller; the event rides this (tracked) aggregate into the outbox in the same transaction.
    /// </summary>
    public void RecordRoleRevoked(string roleCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);

        RaiseIntegrationEvent(new RoleRevokedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            UserId: Id,
            Subject: Subject,
            RoleCode: roleCode));
    }
}
