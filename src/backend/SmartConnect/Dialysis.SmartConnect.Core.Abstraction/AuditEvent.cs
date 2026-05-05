namespace Dialysis.SmartConnect;

/// <summary>
/// A recorded system event for the audit trail (analogous to Mirth Events View).
/// </summary>
public sealed class AuditEvent
{
    public required Guid Id { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required AuditEventCategory Category { get; init; }

    public required AuditEventLevel Level { get; init; }

    public Guid? FlowId { get; init; }

    public string? UserId { get; init; }

    public required string Summary { get; init; }

    public string? AttributesJson { get; init; }
}

public enum AuditEventCategory
{
    FlowDeployed = 0,
    FlowUndeployed = 1,
    FlowStarted = 2,
    FlowStopped = 3,
    FlowPaused = 4,
    ConfigChanged = 5,
    UserAction = 6,
    Error = 7,
}

public enum AuditEventLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
}
