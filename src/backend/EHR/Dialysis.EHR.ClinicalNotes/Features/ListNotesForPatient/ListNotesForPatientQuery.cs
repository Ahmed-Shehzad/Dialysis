using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;

/// <summary>
/// Wire shape for one clinical note row on the chart's Notes section. Mirrors the
/// SOAP-structured aggregate; <c>Status</c> is sent as the enum int (1 Draft / 2 Signed /
/// 3 Amended / 4 EnteredInError) so the SPA tone map is the source of truth.
/// </summary>
public sealed record ClinicalNoteListItem
{
    /// <summary>
    /// Wire shape for one clinical note row on the chart's Notes section. Mirrors the
    /// SOAP-structured aggregate; <c>Status</c> is sent as the enum int (1 Draft / 2 Signed /
    /// 3 Amended / 4 EnteredInError) so the SPA tone map is the source of truth.
    /// </summary>
    public ClinicalNoteListItem(Guid Id,
        Guid EncounterId,
        Guid AuthoringProviderId,
        int Status,
        DateTime CreatedAtUtc,
        DateTime? SignedAtUtc,
        string Subjective,
        string Objective,
        string Assessment,
        string Plan)
    {
        this.Id = Id;
        this.EncounterId = EncounterId;
        this.AuthoringProviderId = AuthoringProviderId;
        this.Status = Status;
        this.CreatedAtUtc = CreatedAtUtc;
        this.SignedAtUtc = SignedAtUtc;
        this.Subjective = Subjective;
        this.Objective = Objective;
        this.Assessment = Assessment;
        this.Plan = Plan;
    }
    public Guid Id { get; init; }
    public Guid EncounterId { get; init; }
    public Guid AuthoringProviderId { get; init; }
    public int Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? SignedAtUtc { get; init; }
    public string Subjective { get; init; }
    public string Objective { get; init; }
    public string Assessment { get; init; }
    public string Plan { get; init; }
    public void Deconstruct(out Guid Id, out Guid EncounterId, out Guid AuthoringProviderId, out int Status, out DateTime CreatedAtUtc, out DateTime? SignedAtUtc, out string Subjective, out string Objective, out string Assessment, out string Plan)
    {
        Id = this.Id;
        EncounterId = this.EncounterId;
        AuthoringProviderId = this.AuthoringProviderId;
        Status = this.Status;
        CreatedAtUtc = this.CreatedAtUtc;
        SignedAtUtc = this.SignedAtUtc;
        Subjective = this.Subjective;
        Objective = this.Objective;
        Assessment = this.Assessment;
        Plan = this.Plan;
    }
}

/// <summary>
/// Lists the most-recent clinical notes authored for a patient, across encounters.
/// Used by the EHR chart's Notes section. Gated by <see cref="EhrPermissions.ChartRead"/>
/// — same gate as the chart-summary view.
/// </summary>
public sealed record ListNotesForPatientQuery : IQuery<IReadOnlyList<ClinicalNoteListItem>>, IPermissionedCommand
{
    /// <summary>
    /// Lists the most-recent clinical notes authored for a patient, across encounters.
    /// Used by the EHR chart's Notes section. Gated by <see cref="EhrPermissions.ChartRead"/>
    /// — same gate as the chart-summary view.
    /// </summary>
    public ListNotesForPatientQuery(Guid PatientId, int Take = 20)
    {
        this.PatientId = PatientId;
        this.Take = Take;
    }
    public string RequiredPermission => EhrPermissions.ChartRead;
    public Guid PatientId { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out Guid PatientId, out int Take)
    {
        PatientId = this.PatientId;
        Take = this.Take;
    }
}
