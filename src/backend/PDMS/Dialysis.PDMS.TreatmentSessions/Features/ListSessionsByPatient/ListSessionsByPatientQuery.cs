using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessions;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessionsByPatient;

/// <summary>
/// Patient-scoped read of recent dialysis sessions. Filters the repository's
/// <c>ListByPatientAsync</c> by patient id and a lookback window. Reuses the wider
/// <see cref="DialysisSessionListItem"/> shape so the SPA can render with the same row
/// component as the operator sessions list.
/// </summary>
public sealed record ListSessionsByPatientQuery(Guid PatientId, int LookbackDays = 90, int Take = 20)
    : IQuery<IReadOnlyList<DialysisSessionListItem>>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionRead;
}
