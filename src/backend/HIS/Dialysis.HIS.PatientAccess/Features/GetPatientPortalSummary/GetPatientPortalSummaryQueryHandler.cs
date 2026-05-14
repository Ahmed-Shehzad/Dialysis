using Dialysis.CQRS.Queries;
using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed class GetPatientPortalSummaryQueryHandler(IPatientPortalReadModel readModel)
    : IQueryHandler<GetPatientPortalSummaryQuery, PatientPortalSummaryDto>
{
    public Task<PatientPortalSummaryDto> Handle(GetPatientPortalSummaryQuery request, CancellationToken cancellationToken)
        => readModel.GetSummaryAsync(request.PatientId, cancellationToken);
}
