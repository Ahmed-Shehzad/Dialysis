using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.CheckInPatient;

public sealed record CheckInPatientCommand : ICommand, IPermissionedCommand
{
    public CheckInPatientCommand(Guid AppointmentId) => this.AppointmentId = AppointmentId;
    public string RequiredPermission => EhrPermissions.AppointmentCheckIn;
    public Guid AppointmentId { get; init; }
    public void Deconstruct(out Guid appointmentId) => appointmentId = this.AppointmentId;
}
