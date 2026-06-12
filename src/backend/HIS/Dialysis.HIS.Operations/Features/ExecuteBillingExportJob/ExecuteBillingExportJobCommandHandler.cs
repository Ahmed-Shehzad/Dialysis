using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.ExecuteBillingExportJob;

public sealed class ExecuteBillingExportJobCommandHandler : ICommandHandler<ExecuteBillingExportJobCommand>
{
    private readonly IBillingExportJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    public ExecuteBillingExportJobCommandHandler(IBillingExportJobRepository jobs,
        IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(ExecuteBillingExportJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Billing export job {request.JobId} was not found.");

        // Throws DomainException (→ 4xx) if the job is no longer Queued.
        job.RequeueForExecution(DateTime.UtcNow);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
