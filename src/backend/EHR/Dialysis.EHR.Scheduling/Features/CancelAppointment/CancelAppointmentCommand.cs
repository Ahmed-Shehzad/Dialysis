using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid AppointmentId, string ReasonCode)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.AppointmentCancel;
}
