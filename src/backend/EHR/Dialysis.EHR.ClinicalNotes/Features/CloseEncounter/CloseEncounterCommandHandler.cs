using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.CloseEncounter;

public sealed class CloseEncounterCommandHandler : ICommandHandler<CloseEncounterCommand, Unit>
{
    private readonly IEncounterRepository _encounters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public CloseEncounterCommandHandler(IEncounterRepository encounters,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _encounters = encounters;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(CloseEncounterCommand request, CancellationToken cancellationToken)
    {
        var encounter = await _encounters.GetAsync(request.EncounterId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Encounter '{request.EncounterId}' not found.");
        encounter.Close(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
