using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand : ICommand, IPermissionedCommand
{
    public RescheduleAppointmentCommand(Guid AppointmentId, DateTime NewStartUtc, DateTime NewEndUtc)
    {
        this.AppointmentId = AppointmentId;
        this.NewStartUtc = NewStartUtc;
        this.NewEndUtc = NewEndUtc;
    }
    public string RequiredPermission => EhrPermissions.AppointmentReschedule;
    public Guid AppointmentId { get; init; }
    public DateTime NewStartUtc { get; init; }
    public DateTime NewEndUtc { get; init; }
    public void Deconstruct(out Guid AppointmentId, out DateTime NewStartUtc, out DateTime NewEndUtc)
    {
        AppointmentId = this.AppointmentId;
        NewStartUtc = this.NewStartUtc;
        NewEndUtc = this.NewEndUtc;
    }
}
