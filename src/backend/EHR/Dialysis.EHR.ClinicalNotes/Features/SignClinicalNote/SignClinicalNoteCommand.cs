using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;

public sealed record SignClinicalNoteCommand(Guid NoteId, Guid SigningProviderId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ClinicalNoteSign;
}
