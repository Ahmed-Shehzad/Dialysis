using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.IntegrationEvents.Billing;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.SubmitBillingExportJob;

public sealed class SubmitBillingExportJobCommandHandler(
    IBillingExportJobRepository jobs,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitBillingExportJobCommand, Guid>
{
    public async Task<Guid> Handle(SubmitBillingExportJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var now = DateTime.UtcNow;
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        jobs.Add(new BillingExportJob
        {
            Id = id,
            PayerCode = request.PayerCode.Trim(),
            StatusCode = "Queued",
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            SubmittedAtUtc = now,
            Notes = notes,
        });

        var @event = new BillingExportJobQueuedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: now,
            JobId: id,
            PayerCode: request.PayerCode.Trim(),
            PeriodStart: request.PeriodStart,
            PeriodEnd: request.PeriodEnd,
            Notes: notes);

        await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
