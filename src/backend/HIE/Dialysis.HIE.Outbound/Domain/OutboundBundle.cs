namespace Dialysis.HIE.Outbound.Domain;

/// <summary>
/// A FHIR resource queued for delivery to an external partner. State transitions:
/// <c>Pending</c> → <c>Delivered</c> (on 2xx) or <c>Failed</c> (after retry budget exhausted).
/// </summary>
public sealed class OutboundBundle
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string ResourceType { get; private set; } = string.Empty;
    public string LogicalId { get; private set; } = string.Empty;
    public string PartnerId { get; private set; } = string.Empty;
    public string FhirJson { get; private set; } = string.Empty;
    public OutboundBundleStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? LastFailureReason { get; private set; }

    private OutboundBundle() { }

    public OutboundBundle(Guid patientId, string resourceType, string logicalId, string partnerId, string fhirJson, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirJson);
        Id = Guid.NewGuid();
        PatientId = patientId;
        ResourceType = resourceType;
        LogicalId = logicalId;
        PartnerId = partnerId;
        FhirJson = fhirJson;
        Status = OutboundBundleStatus.Pending;
        Attempts = 0;
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc;
    }

    public void MarkDelivered(DateTime atUtc)
    {
        Status = OutboundBundleStatus.Delivered;
        DeliveredAtUtc = atUtc;
        Attempts += 1;
    }

    public void MarkAttemptFailed(string reason, DateTime nextAttemptAtUtc, int maxAttempts)
    {
        Attempts += 1;
        LastFailureReason = reason;
        if (Attempts >= maxAttempts)
        {
            Status = OutboundBundleStatus.Failed;
            NextAttemptAtUtc = nextAttemptAtUtc;
            return;
        }
        NextAttemptAtUtc = nextAttemptAtUtc;
    }
}

public enum OutboundBundleStatus
{
    Pending = 1,
    Delivered = 2,
    Failed = 3,
}
