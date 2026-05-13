using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.CloseEncounter;

public sealed class CloseEncounterCommandHandler(
    IEncounterRepository encounters,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CloseEncounterCommand, Unit>
{
    public async Task<Unit> Handle(CloseEncounterCommand request, CancellationToken cancellationToken)
    {
        var encounter = await encounters.GetAsync(request.EncounterId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Encounter '{request.EncounterId}' not found.");
        encounter.Close(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
