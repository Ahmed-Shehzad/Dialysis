using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.AbortSession;

public sealed record AbortSessionCommand(Guid SessionId, string ReasonCode)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionAbort;
}
