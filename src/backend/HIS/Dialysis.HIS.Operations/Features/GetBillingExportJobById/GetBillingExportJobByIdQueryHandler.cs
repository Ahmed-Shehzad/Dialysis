using Dialysis.CQRS.Queries;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed class GetBillingExportJobByIdQueryHandler(IBillingExportJobRepository jobs)
    : IQueryHandler<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>
{
    public async Task<BillingExportJobStatusDto?> Handle(GetBillingExportJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await jobs.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);
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
