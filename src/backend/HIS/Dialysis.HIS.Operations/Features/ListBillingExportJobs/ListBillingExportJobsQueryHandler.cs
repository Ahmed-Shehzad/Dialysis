using Dialysis.CQRS.Queries;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.ListBillingExportJobs;

public sealed class ListBillingExportJobsQueryHandler(IBillingExportJobRepository jobs)
    : IQueryHandler<ListBillingExportJobsQuery, IReadOnlyList<BillingExportJobRow>>
{
    public async Task<IReadOnlyList<BillingExportJobRow>> HandleAsync(
        ListBillingExportJobsQuery request, CancellationToken cancellationToken)
    {
        BillingExportJobStatus? status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : BillingExportJobStatus.FromName(request.Status);

        var rows = await jobs.ListAsync(status, request.Take, cancellationToken).ConfigureAwait(false);
        return rows
            .Select(j => new BillingExportJobRow(
                j.Id,
                j.PayerCode.Value,
                j.Status.Name,
                j.Period.Start,
                j.Period.End,
                j.SubmittedAtUtc,
                j.CompletedAtUtc,
                j.Notes))
            .ToArray();
    }
}
