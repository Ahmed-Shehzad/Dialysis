using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.AttachDiagnosis;

public sealed class AttachDiagnosisCommandHandler : ICommandHandler<AttachDiagnosisCommand, Unit>
{
    private readonly IEncounterRepository _encounters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public AttachDiagnosisCommandHandler(IEncounterRepository encounters,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _encounters = encounters;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(AttachDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var encounter = await _encounters.GetAsync(request.EncounterId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Encounter '{request.EncounterId}' not found.");
        encounter.AttachDiagnosis(request.Icd10Code, request.Rank, request.Display, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
