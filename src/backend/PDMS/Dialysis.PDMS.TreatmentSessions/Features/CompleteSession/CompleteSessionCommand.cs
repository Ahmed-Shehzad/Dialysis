using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;

public sealed record CompleteSessionCommand : ICommand, IPermissionedCommand
{
    public CompleteSessionCommand(Guid SessionId, decimal AchievedUfVolumeLiters)
    {
        this.SessionId = SessionId;
        this.AchievedUfVolumeLiters = AchievedUfVolumeLiters;
    }
    public string RequiredPermission => PdmsPermissions.SessionComplete;
    public Guid SessionId { get; init; }
    public decimal AchievedUfVolumeLiters { get; init; }
    public void Deconstruct(out Guid SessionId, out decimal AchievedUfVolumeLiters)
    {
        SessionId = this.SessionId;
        AchievedUfVolumeLiters = this.AchievedUfVolumeLiters;
    }
}
