using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;

public sealed class RecordClinicalDecisionSupportEvaluationCommandHandler : ICommandHandler<RecordClinicalDecisionSupportEvaluationCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RecordClinicalDecisionSupportEvaluationCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RecordClinicalDecisionSupportEvaluationCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddClinicalDecisionSupportEvaluation(
            new RaClinicalDecisionSupportEvaluation
            {
                Id = id,
                PatientId = request.PatientId,
                ChecksAppliedJson = request.ChecksAppliedJson.Trim(),
                SafeToProceed = request.SafeToProceed,
                EvaluatedAtUtc = DateTime.UtcNow,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
