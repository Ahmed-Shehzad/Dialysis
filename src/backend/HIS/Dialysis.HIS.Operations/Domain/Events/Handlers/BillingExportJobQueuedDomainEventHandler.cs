using Dialysis.DomainDrivenDesign.DomainEvents;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Domain.Events.Handlers;

/// <summary>
/// First production <see cref="IDomainEventHandler{TEvent}"/>: writes an audit row when a billing export
/// job is queued. Driven by the EF SaveChanges interceptor post-commit so audit only lands when the
/// aggregate write durably succeeded.
/// </summary>
public sealed class BillingExportJobQueuedDomainEventHandler : IDomainEventHandler<BillingExportJobQueuedDomainEvent>
{
    private readonly IBillingExportJobAuditRepository _audits;
    /// <summary>
    /// First production <see cref="IDomainEventHandler{TEvent}"/>: writes an audit row when a billing export
    /// job is queued. Driven by the EF SaveChanges interceptor post-commit so audit only lands when the
    /// aggregate write durably succeeded.
    /// </summary>
    public BillingExportJobQueuedDomainEventHandler(IBillingExportJobAuditRepository audits) => _audits = audits;
    public async Task HandleAsync(BillingExportJobQueuedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var audit = new BillingExportJobAudit
        {
            Id = Guid.CreateVersion7(),
            JobId = domainEvent.JobId,
            PayerCode = domainEvent.PayerCode,
            PeriodStart = domainEvent.PeriodStart,
            PeriodEnd = domainEvent.PeriodEnd,
            QueuedAtUtc = domainEvent.QueuedAtUtc,
            RecordedAtUtc = DateTime.UtcNow,
        };

        await _audits.RecordAsync(audit, cancellationToken).ConfigureAwait(false);
    }
}
