using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Identity.Contracts.Integration;

/// <summary>Emitted when a user account is provisioned in the Identity module (system of record for users).</summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid UserId,
    string Subject,
    string DisplayName,
    string? Email) : IIntegrationEvent;

/// <summary>Emitted when a user account is deactivated; subscribers should stop trusting cached permissions for the subject.</summary>
public sealed record UserDeactivatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid UserId,
    string Subject) : IIntegrationEvent;

/// <summary>
/// Emitted when a role is assigned to a user. Each module is expected to cache the resulting
/// permission state locally so authorization decisions can be made without a synchronous call back to Identity.
/// </summary>
public sealed record RoleAssignedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid UserId,
    string Subject,
    string RoleCode,
    IReadOnlyList<string> Permissions) : IIntegrationEvent;

/// <summary>Emitted when a role is revoked from a user; subscribers should drop the associated cached permissions.</summary>
public sealed record RoleRevokedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid UserId,
    string Subject,
    string RoleCode) : IIntegrationEvent;
