using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.PauseSession;

/// <summary>Pauses an in-progress session (machine standby), starting an excluded pause span.</summary>
public sealed record PauseSessionCommand : ICommand, IPermissionedCommand
{
    /// <summary>Pauses an in-progress session (machine standby).</summary>
    public PauseSessionCommand(Guid SessionId) => this.SessionId = SessionId;
    public string RequiredPermission => PdmsPermissions.SessionPause;
    public Guid SessionId { get; init; }
    public void Deconstruct(out Guid SessionId) => SessionId = this.SessionId;
}
