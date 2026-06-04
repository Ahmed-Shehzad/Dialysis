using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.AbortSession;

public sealed record AbortSessionCommand : ICommand, IPermissionedCommand
{
    public AbortSessionCommand(Guid SessionId, string ReasonCode)
    {
        this.SessionId = SessionId;
        this.ReasonCode = ReasonCode;
    }
    public string RequiredPermission => PdmsPermissions.SessionAbort;
    public Guid SessionId { get; init; }
    public string ReasonCode { get; init; }
    public void Deconstruct(out Guid SessionId, out string ReasonCode)
    {
        SessionId = this.SessionId;
        ReasonCode = this.ReasonCode;
    }
}
