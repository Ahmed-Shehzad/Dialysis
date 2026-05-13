using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(Guid AppointmentId, DateTime NewStartUtc, DateTime NewEndUtc)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.AppointmentReschedule;
}
