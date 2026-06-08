using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents.Billing;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Domain.Events;
using Dialysis.HIS.Operations.Domain.ValueObjects;

namespace Dialysis.HIS.Operations.Domain;

/// <summary>
/// Aggregate root: facility-operations trigger for a payer-billing export window. HIS owns only the queue
/// surface; the actual claim filing lives in <c>Dialysis.EHR.Billing</c> and consumes
/// <see cref="BillingExportJobQueuedIntegrationEvent"/>.
/// </summary>
public sealed class BillingExportJob : AggregateRoot<Guid>
{
    public PayerCode PayerCode { get; private set; } = null!;
    public BillingExportJobStatus Status { get; private set; } = BillingExportJobStatus.Queued;
    public BillingPeriod Period { get; private set; } = null!;
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    private BillingExportJob()
    {
    }

    private BillingExportJob(Guid id) : base(id)
    {
    }

    public static BillingExportJob Queue(PayerCode payer, BillingPeriod period, string? notes, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payer);
        ArgumentNullException.ThrowIfNull(period);
        if (notes is not null && notes.Length > 500)
            throw new DomainException("BillingExportJob Notes must be 500 chars or fewer.");

        var job = new BillingExportJob(Guid.CreateVersion7())
        {
            PayerCode = payer,
            Period = period,
            Status = BillingExportJobStatus.Queued,
            SubmittedAtUtc = nowUtc,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        job.RaiseIntegrationEvent(new BillingExportJobQueuedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            JobId: job.Id,
            PayerCode: payer.Value,
            PeriodStart: period.Start,
            PeriodEnd: period.End,
            Notes: job.Notes));

        job.RaiseDomainEvent(new BillingExportJobQueuedDomainEvent(
            JobId: job.Id,
            PayerCode: payer.Value,
            PeriodStart: period.Start,
            PeriodEnd: period.End,
            QueuedAtUtc: nowUtc));

        return job;
    }

    /// <summary>
    /// Operator-triggered (re-)dispatch: re-raises <see cref="BillingExportJobQueuedIntegrationEvent"/>
    /// so EHR's billing pipeline (re-)assembles the EDI 837 batch. Valid only while the job is still
    /// <see cref="BillingExportJobStatus.Queued"/> — a Completed/Failed job is terminal and cannot be
    /// re-executed. Does not change persisted state; the status only advances when EHR reports back.
    /// </summary>
    public void RequeueForExecution(DateTime nowUtc)
    {
        if (Status != BillingExportJobStatus.Queued)
            throw new DomainException($"BillingExportJob cannot be executed from status {Status.Name}.");

        RaiseIntegrationEvent(new BillingExportJobQueuedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            JobId: Id,
            PayerCode: PayerCode.Value,
            PeriodStart: Period.Start,
            PeriodEnd: Period.End,
            Notes: Notes));
    }

    public void MarkCompleted(DateTime nowUtc)
    {
        if (Status != BillingExportJobStatus.Queued)
            throw new DomainException($"BillingExportJob cannot be completed from status {Status.Name}.");
        Status = BillingExportJobStatus.Completed;
        CompletedAtUtc = nowUtc;
    }

    public void MarkFailed(DateTime nowUtc)
    {
        if (Status != BillingExportJobStatus.Queued)
            throw new DomainException($"BillingExportJob cannot be failed from status {Status.Name}.");
        Status = BillingExportJobStatus.Failed;
        CompletedAtUtc = nowUtc;
    }
}
