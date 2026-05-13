using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.StartEncounter;

public sealed class StartEncounterCommandHandler(
    IEncounterRepository encounters,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<StartEncounterCommand, Guid>
{
    public async Task<Guid> Handle(StartEncounterCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var encounter = Encounter.Open(
            id,
            request.PatientId,
            request.ProviderId,
            request.EncounterClassCode,
            timeProvider.GetUtcNow().UtcDateTime,
            request.AppointmentId);
        encounters.Add(encounter);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
