using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Clinical alert (e.g. hypotension risk). Created by Alerting from HypotensionRiskRaised.
/// </summary>
public sealed class Alert : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public ObservationId? ObservationId { get; private set; }
    public string Severity { get; private set; } = "Warning";
    public string Message { get; private set; } = "";
    public AlertStatus Status { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public string? AcknowledgedBy { get; private set; }

    private Alert()
    {
        TenantId = null!;
        PatientId = null!;
        Status = AlertStatus.Active;
    }

    public static Alert Create(TenantId tenantId, PatientId patientId, string message, ObservationId? observationId = null, string severity = "Warning")
    {
        return new Alert
        {
            TenantId = tenantId,
            PatientId = patientId,
            ObservationId = observationId,
            Severity = severity,
            Message = message,
            Status = AlertStatus.Active
        };
    }

    public void Acknowledge(string? acknowledgedBy = null)
    {
        if (Status == AlertStatus.Acknowledged)
            return;
        Status = AlertStatus.Acknowledged;
        AcknowledgedAtUtc = DateTime.UtcNow;
        AcknowledgedBy = acknowledgedBy;
        ApplyUpdateDateTime();
    }
}

public enum AlertStatus
{
    Active = 0,
    Acknowledged = 1
}
