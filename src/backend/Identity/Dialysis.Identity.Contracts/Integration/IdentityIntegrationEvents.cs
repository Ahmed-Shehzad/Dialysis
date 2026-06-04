using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Identity.Contracts.Integration;

/// <summary>Emitted when a user account is provisioned in the Identity module (system of record for users).</summary>
public sealed record UserRegisteredIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted when a user account is provisioned in the Identity module (system of record for users).</summary>
    public UserRegisteredIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid UserId,
        string Subject,
        string DisplayName,
        string? Email)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.UserId = UserId;
        this.Subject = Subject;
        this.DisplayName = DisplayName;
        this.Email = Email;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid UserId { get; init; }
    public string Subject { get; init; }
    public string DisplayName { get; init; }
    public string? Email { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid UserId, out string Subject, out string DisplayName, out string? Email)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        UserId = this.UserId;
        Subject = this.Subject;
        DisplayName = this.DisplayName;
        Email = this.Email;
    }
}

/// <summary>Emitted when a user account is deactivated; subscribers should stop trusting cached permissions for the subject.</summary>
public sealed record UserDeactivatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted when a user account is deactivated; subscribers should stop trusting cached permissions for the subject.</summary>
    public UserDeactivatedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid UserId,
        string Subject)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.UserId = UserId;
        this.Subject = Subject;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid UserId { get; init; }
    public string Subject { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid UserId, out string Subject)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        UserId = this.UserId;
        Subject = this.Subject;
    }
}

/// <summary>
/// Emitted when a role is assigned to a user. Each module is expected to cache the resulting
/// permission state locally so authorization decisions can be made without a synchronous call back to Identity.
/// </summary>
public sealed record RoleAssignedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a role is assigned to a user. Each module is expected to cache the resulting
    /// permission state locally so authorization decisions can be made without a synchronous call back to Identity.
    /// </summary>
    public RoleAssignedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid UserId,
        string Subject,
        string RoleCode,
        IReadOnlyList<string> Permissions)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.UserId = UserId;
        this.Subject = Subject;
        this.RoleCode = RoleCode;
        this.Permissions = Permissions;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid UserId { get; init; }
    public string Subject { get; init; }
    public string RoleCode { get; init; }
    public IReadOnlyList<string> Permissions { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid UserId, out string Subject, out string RoleCode, out IReadOnlyList<string> Permissions)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        UserId = this.UserId;
        Subject = this.Subject;
        RoleCode = this.RoleCode;
        Permissions = this.Permissions;
    }
}

/// <summary>Emitted when a role is revoked from a user; subscribers should drop the associated cached permissions.</summary>
public sealed record RoleRevokedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted when a role is revoked from a user; subscribers should drop the associated cached permissions.</summary>
    public RoleRevokedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid UserId,
        string Subject,
        string RoleCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.UserId = UserId;
        this.Subject = Subject;
        this.RoleCode = RoleCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid UserId { get; init; }
    public string Subject { get; init; }
    public string RoleCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid UserId, out string Subject, out string RoleCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        UserId = this.UserId;
        Subject = this.Subject;
        RoleCode = this.RoleCode;
    }
}
