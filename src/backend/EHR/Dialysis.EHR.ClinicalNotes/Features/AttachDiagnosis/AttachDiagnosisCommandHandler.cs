using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.AttachDiagnosis;

public sealed class AttachDiagnosisCommandHandler(
    IEncounterRepository encounters,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AttachDiagnosisCommand, Unit>
{
    public async Task<Unit> HandleAsync(AttachDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var encounter = await encounters.GetAsync(request.EncounterId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Encounter '{request.EncounterId}' not found.");
        encounter.AttachDiagnosis(request.Icd10Code, request.Rank, request.Display, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
