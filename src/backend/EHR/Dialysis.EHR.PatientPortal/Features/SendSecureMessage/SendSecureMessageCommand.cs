using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.SendSecureMessage;

public sealed record SendSecureMessageCommand : ICommand<Guid>, IPermissionedCommand
{
    public SendSecureMessageCommand(Guid PatientId,
        Guid? ThreadId,
        Guid? TargetProviderId,
        SecureMessageDirection Direction,
        string Subject,
        string Body)
    {
        this.PatientId = PatientId;
        this.ThreadId = ThreadId;
        this.TargetProviderId = TargetProviderId;
        this.Direction = Direction;
        this.Subject = Subject;
        this.Body = Body;
    }
    public string RequiredPermission => EhrPermissions.PortalMessageSend;
    public Guid PatientId { get; init; }
    public Guid? ThreadId { get; init; }
    public Guid? TargetProviderId { get; init; }
    public SecureMessageDirection Direction { get; init; }
    public string Subject { get; init; }
    public string Body { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid? ThreadId, out Guid? TargetProviderId, out SecureMessageDirection Direction, out string Subject, out string Body)
    {
        PatientId = this.PatientId;
        ThreadId = this.ThreadId;
        TargetProviderId = this.TargetProviderId;
        Direction = this.Direction;
        Subject = this.Subject;
        Body = this.Body;
    }
}
