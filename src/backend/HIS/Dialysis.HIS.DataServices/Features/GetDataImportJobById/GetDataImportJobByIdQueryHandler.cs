using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.GetDataImportJobById;

public sealed class GetDataImportJobByIdQueryHandler(IDataImportJobRepository jobs)
    : IQueryHandler<GetDataImportJobByIdQuery, DataImportJobStatusDto?>
{
    public async Task<DataImportJobStatusDto?> Handle(GetDataImportJobByIdQuery request, CancellationToken cancellationToken)
    {
        var row = await jobs.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return row is null
            ? null
            : new DataImportJobStatusDto(row.Id, row.SourceDescription, row.SubmittedAtUtc, row.StatusCode, row.ValidationSummary);
    }
}
