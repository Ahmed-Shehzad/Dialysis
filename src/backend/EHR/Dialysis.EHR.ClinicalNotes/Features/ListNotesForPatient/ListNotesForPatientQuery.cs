using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;

/// <summary>
/// Wire shape for one clinical note row on the chart's Notes section. Mirrors the
/// SOAP-structured aggregate; <c>Status</c> is sent as the enum int (1 Draft / 2 Signed /
/// 3 Amended / 4 EnteredInError) so the SPA tone map is the source of truth.
/// </summary>
public sealed record ClinicalNoteListItem(
    Guid Id,
    Guid EncounterId,
    Guid AuthoringProviderId,
    int Status,
    DateTime CreatedAtUtc,
    DateTime? SignedAtUtc,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan);

/// <summary>
/// Lists the most-recent clinical notes authored for a patient, across encounters.
/// Used by the EHR chart's Notes section. Gated by <see cref="EhrPermissions.ChartRead"/>
/// — same gate as the chart-summary view.
/// </summary>
public sealed record ListNotesForPatientQuery(Guid PatientId, int Take = 20)
    : IQuery<IReadOnlyList<ClinicalNoteListItem>>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ChartRead;
}
