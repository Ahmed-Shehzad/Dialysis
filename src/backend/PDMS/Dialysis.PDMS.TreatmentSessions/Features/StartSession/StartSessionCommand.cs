using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.StartSession;

public sealed record StartSessionCommand(Guid SessionId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionStart;
}
