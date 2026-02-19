namespace Dialysis.Alarm.Infrastructure.ReadModels;

/// <summary>
/// Read-only projection of Alarm for query operations. Maps to the Alarms table.
/// </summary>
public sealed class AlarmReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string? AlarmType { get; init; }
    public string? SourceCode { get; init; }
    public string? SourceLimits { get; init; }
    public string? Priority { get; init; }
    public string? InterpretationType { get; init; }
    public string? Abnormality { get; init; }
    public string EventPhase { get; init; } = string.Empty;
    public string AlarmState { get; init; } = string.Empty;
    public string ActivityState { get; init; } = string.Empty;
    public string? DeviceId { get; init; }
    public string? SessionId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
