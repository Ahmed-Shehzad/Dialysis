using Dialysis.CQRS.Queries;
using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed class GetPatientPortalSummaryQueryHandler : IQueryHandler<GetPatientPortalSummaryQuery, PatientPortalSummaryDto>
{
    private readonly IPatientPortalReadModel _readModel;
    public GetPatientPortalSummaryQueryHandler(IPatientPortalReadModel readModel) => _readModel = readModel;
    public Task<PatientPortalSummaryDto> HandleAsync(GetPatientPortalSummaryQuery request, CancellationToken cancellationToken)
        => _readModel.GetSummaryAsync(request.PatientId, cancellationToken);
}
