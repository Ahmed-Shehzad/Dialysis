using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.StartSession;

public sealed record StartSessionCommand : ICommand, IPermissionedCommand
{
    public StartSessionCommand(Guid SessionId) => this.SessionId = SessionId;
    public string RequiredPermission => PdmsPermissions.SessionStart;
    public Guid SessionId { get; init; }
    public void Deconstruct(out Guid sessionId) => sessionId = SessionId;
}
