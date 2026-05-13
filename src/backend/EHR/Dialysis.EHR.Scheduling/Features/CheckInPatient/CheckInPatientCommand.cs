using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.CheckInPatient;

public sealed record CheckInPatientCommand(Guid AppointmentId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.AppointmentCheckIn;
}
