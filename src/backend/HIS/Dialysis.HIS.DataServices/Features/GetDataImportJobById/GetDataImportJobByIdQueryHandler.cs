using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.GetDataImportJobById;

public sealed class GetDataImportJobByIdQueryHandler : IQueryHandler<GetDataImportJobByIdQuery, DataImportJobStatusDto?>
{
    private readonly IDataImportJobRepository _jobs;
    public GetDataImportJobByIdQueryHandler(IDataImportJobRepository jobs) => _jobs = jobs;
    public async Task<DataImportJobStatusDto?> HandleAsync(GetDataImportJobByIdQuery request, CancellationToken cancellationToken)
    {
        var row = await _jobs.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return row is null
            ? null
            : new DataImportJobStatusDto(row.Id, row.SourceDescription, row.SubmittedAtUtc, row.StatusCode, row.ValidationSummary);
    }
}
