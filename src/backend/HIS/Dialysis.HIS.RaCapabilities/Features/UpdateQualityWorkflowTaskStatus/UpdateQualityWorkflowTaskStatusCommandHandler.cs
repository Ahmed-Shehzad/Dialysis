using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;

public sealed class UpdateQualityWorkflowTaskStatusCommandHandler : ICommandHandler<UpdateQualityWorkflowTaskStatusCommand>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateQualityWorkflowTaskStatusCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(UpdateQualityWorkflowTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var ok = await _store
            .TryUpdateQualityWorkflowTaskStatusAsync(request.TaskId, request.NewStatusCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
            throw new InvalidOperationException("Quality workflow task not found.");
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
