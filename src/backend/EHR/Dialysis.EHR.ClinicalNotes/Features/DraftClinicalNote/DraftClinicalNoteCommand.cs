using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;

public sealed record DraftClinicalNoteCommand(
    Guid EncounterId,
    Guid PatientId,
    Guid AuthoringProviderId,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ClinicalNoteWrite;
}
