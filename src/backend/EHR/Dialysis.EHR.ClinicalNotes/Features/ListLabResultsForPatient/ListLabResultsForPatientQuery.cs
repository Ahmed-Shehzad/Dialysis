using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;

/// <summary>
/// Wire shape for one lab result row. <c>AbnormalFlag</c> ships as the enum int
/// (1 Normal / 2 Low / 3 High / 4 Critical / 5 AbnormalNos) so the SPA tone map is
/// the source of truth for badge colour.
/// </summary>
public sealed record LabResultListItem(
    Guid Id,
    Guid LabOrderId,
    string LoincCode,
    string ValueText,
    string? UnitCode,
    string? ReferenceRangeText,
    int AbnormalFlag,
    DateTime ObservedAtUtc);

/// <summary>
/// Lists recent lab results for one patient. Used by the patient-portal Lab results
/// panel and any clinician view that needs a patient-scoped result feed.
/// </summary>
public sealed record ListLabResultsForPatientQuery(Guid PatientId, int LookbackDays = 180, int Take = 50)
    : IQuery<IReadOnlyList<LabResultListItem>>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ChartRead;
}
