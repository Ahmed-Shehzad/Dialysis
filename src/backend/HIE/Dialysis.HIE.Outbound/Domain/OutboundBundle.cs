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

    /// <summary>
    /// TEFCA permitted purpose this disclosure was enqueued under. Travels to the wire so the
    /// per-call IAS JWT can assert it. Null on bundles enqueued before purpose governance — the
    /// dispatcher defaults those to Treatment.
    /// </summary>
    public string? Purpose { get; private set; }

    public OutboundBundleStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? LastFailureReason { get; private set; }

    private OutboundBundle() { }

    public OutboundBundle(Guid patientId, string resourceType, string logicalId, string partnerId, string fhirJson, DateTime createdAtUtc, string? purpose = null)
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
        Purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose.Trim();
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

    /// <summary>
    /// Operator-driven retry of a <see cref="OutboundBundleStatus.Failed"/> bundle. Resets
    /// the status to <see cref="OutboundBundleStatus.Pending"/> with an immediate
    /// <see cref="NextAttemptAtUtc"/> so the dispatcher claims it on its next tick. The
    /// attempt counter is preserved as audit history; the operator is explicitly accepting
    /// that the previous attempts ran. No-ops on bundles that are already delivered.
    /// </summary>
    public void MarkForRetry(DateTime asOfUtc)
    {
        if (Status == OutboundBundleStatus.Delivered) return;
        Status = OutboundBundleStatus.Pending;
        NextAttemptAtUtc = asOfUtc;
    }
}

public enum OutboundBundleStatus
{
    Pending = 1,
    Delivered = 2,
    Failed = 3,
}
