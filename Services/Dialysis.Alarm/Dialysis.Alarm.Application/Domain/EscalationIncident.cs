using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Events;

namespace Dialysis.Alarm.Application.Domain;

/// <summary>
/// Aggregate root representing an alarm escalation incident. Raised when multiple active alarms
/// trigger escalation policy. Raises AlarmEscalationTriggeredEvent for downstream consumers (FHIR, analytics).
/// </summary>
public sealed class EscalationIncident : AggregateRoot
{
    public string? DeviceId { get; private set; }
    public string? SessionId { get; private set; }
    public int ActiveAlarmCount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public TenantId TenantId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private EscalationIncident() { }

    /// <summary>
    /// Records an escalation incident and raises AlarmEscalationTriggeredEvent for post-commit dispatch.
    /// </summary>
    public static EscalationIncident Record(string? deviceId, string? sessionId, int activeAlarmCount, string reason, string? tenantId)
    {
        var incident = new EscalationIncident
        {
            DeviceId = deviceId,
            SessionId = sessionId,
            ActiveAlarmCount = activeAlarmCount,
            Reason = reason ?? string.Empty,
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantId.Default : new TenantId(tenantId),
            OccurredAt = DateTimeOffset.UtcNow
        };

        incident.ApplyEvent(new AlarmEscalationTriggeredEvent(
            deviceId,
            sessionId,
            activeAlarmCount,
            reason ?? string.Empty,
            incident.TenantId.Value));

        return incident;
    }
}
