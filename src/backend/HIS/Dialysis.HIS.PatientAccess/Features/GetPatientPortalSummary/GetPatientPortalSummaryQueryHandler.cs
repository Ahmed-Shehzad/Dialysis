using Dialysis.CQRS.Queries;
using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed class GetPatientPortalSummaryQueryHandler(
    IPatientConsentGate consent,
    IPatientPortalSummaryReadModel readModel)
    : IQueryHandler<GetPatientPortalSummaryQuery, PatientPortalSummaryDto?>
{
    public async Task<PatientPortalSummaryDto?> Handle(GetPatientPortalSummaryQuery request, CancellationToken cancellationToken)
    {
        await consent.EnsureCanViewSummaryAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        return await readModel.GetAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
    }
}
