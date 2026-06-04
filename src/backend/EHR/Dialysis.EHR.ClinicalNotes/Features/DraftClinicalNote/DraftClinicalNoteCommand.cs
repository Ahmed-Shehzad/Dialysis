using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;

public sealed record DraftClinicalNoteCommand : ICommand<Guid>, IPermissionedCommand
{
    public DraftClinicalNoteCommand(Guid EncounterId,
        Guid PatientId,
        Guid AuthoringProviderId,
        string Subjective,
        string Objective,
        string Assessment,
        string Plan)
    {
        this.EncounterId = EncounterId;
        this.PatientId = PatientId;
        this.AuthoringProviderId = AuthoringProviderId;
        this.Subjective = Subjective;
        this.Objective = Objective;
        this.Assessment = Assessment;
        this.Plan = Plan;
    }
    public string RequiredPermission => EhrPermissions.ClinicalNoteWrite;
    public Guid EncounterId { get; init; }
    public Guid PatientId { get; init; }
    public Guid AuthoringProviderId { get; init; }
    public string Subjective { get; init; }
    public string Objective { get; init; }
    public string Assessment { get; init; }
    public string Plan { get; init; }
    public void Deconstruct(out Guid EncounterId, out Guid PatientId, out Guid AuthoringProviderId, out string Subjective, out string Objective, out string Assessment, out string Plan)
    {
        EncounterId = this.EncounterId;
        PatientId = this.PatientId;
        AuthoringProviderId = this.AuthoringProviderId;
        Subjective = this.Subjective;
        Objective = this.Objective;
        Assessment = this.Assessment;
        Plan = this.Plan;
    }
}
