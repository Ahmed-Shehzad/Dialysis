using Dialysis.DomainDrivenDesign.DomainEvents;

namespace Dialysis.HIS.Operations.Domain.Events;

/// <summary>
/// In-bounded-context domain event raised when a billing export job is queued. Distinct from the
/// outbound <c>BillingExportJobQueuedIntegrationEvent</c> in <c>HIS.Contracts</c>: this fires within
/// HIS for audit/projection handlers post-commit.
/// </summary>
public sealed record BillingExportJobQueuedDomainEvent : DomainEvent
{
    /// <summary>
    /// In-bounded-context domain event raised when a billing export job is queued. Distinct from the
    /// outbound <c>BillingExportJobQueuedIntegrationEvent</c> in <c>HIS.Contracts</c>: this fires within
    /// HIS for audit/projection handlers post-commit.
    /// </summary>
    public BillingExportJobQueuedDomainEvent(Guid JobId,
        string PayerCode,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        DateTime QueuedAtUtc)
    {
        this.JobId = JobId;
        this.PayerCode = PayerCode;
        this.PeriodStart = PeriodStart;
        this.PeriodEnd = PeriodEnd;
        this.QueuedAtUtc = QueuedAtUtc;
    }
    public Guid JobId { get; init; }
    public string PayerCode { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public DateTime QueuedAtUtc { get; init; }
    public void Deconstruct(out Guid JobId, out string PayerCode, out DateOnly PeriodStart, out DateOnly PeriodEnd, out DateTime QueuedAtUtc)
    {
        JobId = this.JobId;
        PayerCode = this.PayerCode;
        PeriodStart = this.PeriodStart;
        PeriodEnd = this.PeriodEnd;
        QueuedAtUtc = this.QueuedAtUtc;
    }
}
