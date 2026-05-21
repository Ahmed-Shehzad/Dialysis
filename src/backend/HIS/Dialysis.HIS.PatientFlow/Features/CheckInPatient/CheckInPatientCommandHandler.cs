using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
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
        entry.CheckIn(request.EligibilityAcknowledged);

        var integrationEvent = new PatientCheckedInIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            EntryId: entry.Id,
            PatientId: entry.PatientId,
            PatientName: entry.PatientName,
            Mrn: entry.Mrn,
            CheckedInAtUtc: request.ArrivalTimeUtc);
        await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(integrationEvent), cancellationToken).ConfigureAwait(false);

        // Repository state lives in memory for the demo; SaveChanges only commits the outbox
        // row written above. When the EF-backed repo lands the same call commits both.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
