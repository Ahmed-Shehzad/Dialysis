using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.CloseEncounter;

public sealed record CloseEncounterCommand : ICommand, IPermissionedCommand
{
    public CloseEncounterCommand(Guid EncounterId) => this.EncounterId = EncounterId;
    public string RequiredPermission => EhrPermissions.EncounterClose;
    public Guid EncounterId { get; init; }
    public void Deconstruct(out Guid EncounterId) => EncounterId = this.EncounterId;
}
