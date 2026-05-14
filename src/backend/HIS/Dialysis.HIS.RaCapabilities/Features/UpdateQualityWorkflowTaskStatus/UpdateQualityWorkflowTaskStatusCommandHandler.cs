using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;

public sealed class UpdateQualityWorkflowTaskStatusCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateQualityWorkflowTaskStatusCommand>
{
    public async Task<Unit> HandleAsync(UpdateQualityWorkflowTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var ok = await store
            .TryUpdateQualityWorkflowTaskStatusAsync(request.TaskId, request.NewStatusCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
            throw new InvalidOperationException("Quality workflow task not found.");
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
