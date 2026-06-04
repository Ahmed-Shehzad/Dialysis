using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;

/// <summary>
/// Wire shape for one lab result row. <c>AbnormalFlag</c> ships as the enum int
/// (1 Normal / 2 Low / 3 High / 4 Critical / 5 AbnormalNos) so the SPA tone map is
/// the source of truth for badge colour.
/// </summary>
public sealed record LabResultListItem
{
    /// <summary>
    /// Wire shape for one lab result row. <c>AbnormalFlag</c> ships as the enum int
    /// (1 Normal / 2 Low / 3 High / 4 Critical / 5 AbnormalNos) so the SPA tone map is
    /// the source of truth for badge colour.
    /// </summary>
    public LabResultListItem(Guid Id,
        Guid LabOrderId,
        string LoincCode,
        string ValueText,
        string? UnitCode,
        string? ReferenceRangeText,
        int AbnormalFlag,
        DateTime ObservedAtUtc)
    {
        this.Id = Id;
        this.LabOrderId = LabOrderId;
        this.LoincCode = LoincCode;
        this.ValueText = ValueText;
        this.UnitCode = UnitCode;
        this.ReferenceRangeText = ReferenceRangeText;
        this.AbnormalFlag = AbnormalFlag;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid LabOrderId { get; init; }
    public string LoincCode { get; init; }
    public string ValueText { get; init; }
    public string? UnitCode { get; init; }
    public string? ReferenceRangeText { get; init; }
    public int AbnormalFlag { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid LabOrderId, out string LoincCode, out string ValueText, out string? UnitCode, out string? ReferenceRangeText, out int AbnormalFlag, out DateTime ObservedAtUtc)
    {
        Id = this.Id;
        LabOrderId = this.LabOrderId;
        LoincCode = this.LoincCode;
        ValueText = this.ValueText;
        UnitCode = this.UnitCode;
        ReferenceRangeText = this.ReferenceRangeText;
        AbnormalFlag = this.AbnormalFlag;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}

/// <summary>
/// Lists recent lab results for one patient. Used by the patient-portal Lab results
/// panel and any clinician view that needs a patient-scoped result feed.
/// </summary>
public sealed record ListLabResultsForPatientQuery : IQuery<IReadOnlyList<LabResultListItem>>, IPermissionedCommand
{
    /// <summary>
    /// Lists recent lab results for one patient. Used by the patient-portal Lab results
    /// panel and any clinician view that needs a patient-scoped result feed.
    /// </summary>
    public ListLabResultsForPatientQuery(Guid PatientId, int LookbackDays = 180, int Take = 50)
    {
        this.PatientId = PatientId;
        this.LookbackDays = LookbackDays;
        this.Take = Take;
    }
    public string RequiredPermission => EhrPermissions.ChartRead;
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
