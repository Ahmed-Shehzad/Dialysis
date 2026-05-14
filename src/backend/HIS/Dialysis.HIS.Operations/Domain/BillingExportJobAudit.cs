namespace Dialysis.HIS.Operations.Domain;

/// <summary>
/// Audit projection row written by <c>BillingExportJobQueuedDomainEventHandler</c> when a job is queued.
/// Plain row (not an aggregate) — purely a side-effect record for traceability.
/// </summary>
public sealed class BillingExportJobAudit
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string PayerCode { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateTime QueuedAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
