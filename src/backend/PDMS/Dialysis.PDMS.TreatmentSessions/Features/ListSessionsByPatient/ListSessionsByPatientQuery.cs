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
public sealed record ListSessionsByPatientQuery : IQuery<IReadOnlyList<DialysisSessionListItem>>, IPermissionedCommand
{
    /// <summary>
    /// Patient-scoped read of recent dialysis sessions. Filters the repository's
    /// <c>ListByPatientAsync</c> by patient id and a lookback window. Reuses the wider
    /// <see cref="DialysisSessionListItem"/> shape so the SPA can render with the same row
    /// component as the operator sessions list.
    /// </summary>
    public ListSessionsByPatientQuery(Guid PatientId, int LookbackDays = 90, int Take = 20)
    {
        this.PatientId = PatientId;
        this.LookbackDays = LookbackDays;
        this.Take = Take;
    }
    public string RequiredPermission => PdmsPermissions.SessionRead;
    public Guid PatientId { get; init; }
    public int LookbackDays { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out Guid PatientId, out int LookbackDays, out int Take)
    {
        PatientId = this.PatientId;
        LookbackDays = this.LookbackDays;
        Take = this.Take;
    }
}
