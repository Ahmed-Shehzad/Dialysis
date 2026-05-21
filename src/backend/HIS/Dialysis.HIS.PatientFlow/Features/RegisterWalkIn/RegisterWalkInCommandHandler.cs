using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;

public sealed class RegisterWalkInCommandHandler(
    IPatientQueueRepository repository,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterWalkInCommand, PatientQueueEntryDto>
{
    public async Task<PatientQueueEntryDto> HandleAsync(
        RegisterWalkInCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entry = PatientQueueEntry.WalkIn(
            id: Guid.CreateVersion7(),
            patientId: Guid.CreateVersion7(),
            patientName: request.PatientName.Trim(),
            mrn: request.Mrn.Trim(),
            arrivalUtc: now,
            eligibilityVerified: request.EligibilityVerified);
        repository.Add(entry);

        var integrationEvent = new WalkInRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: now,
            SchemaVersion: 1,
            EntryId: entry.Id,
            PatientId: entry.PatientId,
            PatientName: entry.PatientName,
            Mrn: entry.Mrn,
            EligibilityVerified: entry.EligibilityVerified,
            RegisteredAtUtc: now);
        await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(integrationEvent), cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PatientQueueEntryDto(
            entry.Id,
            entry.PatientId,
            entry.PatientName,
            entry.Mrn,
            entry.ScheduledForUtc,
            "waiting",
            entry.Chair,
            entry.EligibilityVerified);
    }
}
