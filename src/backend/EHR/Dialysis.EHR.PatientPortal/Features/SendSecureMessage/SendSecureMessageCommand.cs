using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.SendSecureMessage;

public sealed record SendSecureMessageCommand(
    Guid PatientId,
    Guid? ThreadId,
    Guid? TargetProviderId,
    SecureMessageDirection Direction,
    string Subject,
    string Body)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PortalMessageSend;
}
