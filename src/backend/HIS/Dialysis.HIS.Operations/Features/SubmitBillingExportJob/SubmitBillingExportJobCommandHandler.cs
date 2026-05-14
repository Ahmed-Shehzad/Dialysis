using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.ValueObjects;
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
        var nowUtc = DateTime.UtcNow;
        var job = BillingExportJob.Queue(
            new PayerCode(request.PayerCode),
            new BillingPeriod(request.PeriodStart, request.PeriodEnd),
            request.Notes,
            nowUtc);

        jobs.Add(job);

        foreach (var @event in job.IntegrationEvents)
        {
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        job.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return job.Id;
    }
}
