using Dialysis.CQRS.Queries;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed class GetBillingExportJobByIdQueryHandler(IBillingExportRepository billing)
    : IQueryHandler<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>
{
    public async Task<BillingExportJobStatusDto?> Handle(GetBillingExportJobByIdQuery request, CancellationToken cancellationToken)
    {
        var row = await billing.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return row is null
            ? null
            : new BillingExportJobStatusDto(row.Id, row.RequestedAtUtc, row.FormatCode, row.StatusCode, row.PayerCode);
    }
}
