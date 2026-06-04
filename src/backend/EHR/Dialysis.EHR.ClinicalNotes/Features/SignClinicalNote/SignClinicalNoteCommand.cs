using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;

public sealed record SignClinicalNoteCommand : ICommand, IPermissionedCommand
{
    public SignClinicalNoteCommand(Guid NoteId, Guid SigningProviderId)
    {
        this.NoteId = NoteId;
        this.SigningProviderId = SigningProviderId;
    }
    public string RequiredPermission => EhrPermissions.ClinicalNoteSign;
    public Guid NoteId { get; init; }
    public Guid SigningProviderId { get; init; }
    public void Deconstruct(out Guid NoteId, out Guid SigningProviderId)
    {
        NoteId = this.NoteId;
        SigningProviderId = this.SigningProviderId;
    }
}
