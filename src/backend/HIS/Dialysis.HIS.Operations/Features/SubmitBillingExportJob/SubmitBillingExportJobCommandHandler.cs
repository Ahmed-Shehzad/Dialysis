using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.Services;
using Dialysis.HIS.Operations.Domain.ValueObjects;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.SubmitBillingExportJob;

public sealed class SubmitBillingExportJobCommandHandler(
    IBillingExportJobRepository jobs,
    BillingExportEligibilityService eligibility,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitBillingExportJobCommand, Guid>
{
    public async Task<Guid> HandleAsync(SubmitBillingExportJobCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var payer = new PayerCode(request.PayerCode);
        var period = new BillingPeriod(request.PeriodStart, request.PeriodEnd);

        await eligibility.EnsureNoQueuedDuplicateAsync(payer, period, cancellationToken).ConfigureAwait(false);

        var job = BillingExportJob.Queue(payer, period, request.Notes, nowUtc);

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
