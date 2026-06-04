using Dialysis.CQRS.Queries;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed class GetBillingExportJobByIdQueryHandler : IQueryHandler<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>
{
    private readonly IBillingExportJobRepository _jobs;
    public GetBillingExportJobByIdQueryHandler(IBillingExportJobRepository jobs) => _jobs = jobs;
    public async Task<BillingExportJobStatusDto?> HandleAsync(GetBillingExportJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return job is null
            ? null
            : new BillingExportJobStatusDto(
                job.Id,
                job.PayerCode.Value,
                job.Status.Name,
                job.Period.Start,
                job.Period.End,
                job.SubmittedAtUtc,
                job.CompletedAtUtc,
                job.Notes);
    }
}
