using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.CloseEncounter;

public sealed record CloseEncounterCommand(Guid EncounterId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.EncounterClose;
}
