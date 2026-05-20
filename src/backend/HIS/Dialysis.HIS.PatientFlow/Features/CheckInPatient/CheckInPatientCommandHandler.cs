using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.CheckInPatient;

public sealed class CheckInPatientCommandHandler(
    IPatientQueueRepository repository,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CheckInPatientCommand, Guid>
{
    public async Task<Guid> HandleAsync(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        var entry = repository.Get(request.EntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");
        entry.CheckIn(request.ArrivalTimeUtc, request.EligibilityAcknowledged);

        foreach (var @event in entry.IntegrationEvents)
        {
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        entry.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
