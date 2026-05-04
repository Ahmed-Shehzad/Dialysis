using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;

public sealed class RecordClinicalDecisionSupportEvaluationCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordClinicalDecisionSupportEvaluationCommand, Guid>
{
    public async Task<Guid> Handle(RecordClinicalDecisionSupportEvaluationCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddClinicalDecisionSupportEvaluation(
            new RaClinicalDecisionSupportEvaluation
            {
                Id = id,
                PatientId = request.PatientId,
                ChecksAppliedJson = request.ChecksAppliedJson.Trim(),
                SafeToProceed = request.SafeToProceed,
                EvaluatedAtUtc = DateTime.UtcNow,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
