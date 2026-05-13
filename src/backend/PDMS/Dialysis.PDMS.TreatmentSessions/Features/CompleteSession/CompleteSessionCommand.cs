using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;

public sealed record CompleteSessionCommand(Guid SessionId, decimal AchievedUfVolumeLiters)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionComplete;
}
