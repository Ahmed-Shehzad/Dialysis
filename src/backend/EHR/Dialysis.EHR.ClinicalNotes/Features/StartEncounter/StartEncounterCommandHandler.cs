using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.StartEncounter;

public sealed class StartEncounterCommandHandler : ICommandHandler<StartEncounterCommand, Guid>
{
    private readonly IEncounterRepository _encounters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public StartEncounterCommandHandler(IEncounterRepository encounters,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _encounters = encounters;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(StartEncounterCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var encounter = Encounter.Open(
            id,
            request.PatientId,
            request.ProviderId,
            request.EncounterClassCode,
            _timeProvider.GetUtcNow().UtcDateTime,
            request.AppointmentId);
        _encounters.Add(encounter);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
