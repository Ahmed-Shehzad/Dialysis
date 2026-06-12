using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.Services;
using Dialysis.HIS.Operations.Domain.ValueObjects;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.SubmitBillingExportJob;

public sealed class SubmitBillingExportJobCommandHandler : ICommandHandler<SubmitBillingExportJobCommand, Guid>
{
    private readonly IBillingExportJobRepository _jobs;
    private readonly BillingExportEligibilityService _eligibility;
    private readonly IUnitOfWork _unitOfWork;
    public SubmitBillingExportJobCommandHandler(IBillingExportJobRepository jobs,
        BillingExportEligibilityService eligibility,
        IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _eligibility = eligibility;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(SubmitBillingExportJobCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var payer = new PayerCode(request.PayerCode);
        var period = new BillingPeriod(request.PeriodStart, request.PeriodEnd);

        await _eligibility.EnsureNoQueuedDuplicateAsync(payer, period, cancellationToken).ConfigureAwait(false);

        var job = BillingExportJob.Queue(payer, period, request.Notes, nowUtc);

        _jobs.Add(job);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return job.Id;
    }
}
