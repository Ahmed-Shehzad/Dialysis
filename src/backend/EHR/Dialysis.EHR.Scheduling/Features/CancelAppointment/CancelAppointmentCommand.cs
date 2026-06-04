using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.CancelAppointment;

public sealed record CancelAppointmentCommand : ICommand, IPermissionedCommand
{
    public CancelAppointmentCommand(Guid AppointmentId, string ReasonCode)
    {
        this.AppointmentId = AppointmentId;
        this.ReasonCode = ReasonCode;
    }
    public string RequiredPermission => EhrPermissions.AppointmentCancel;
    public Guid AppointmentId { get; init; }
    public string ReasonCode { get; init; }
    public void Deconstruct(out Guid AppointmentId, out string ReasonCode)
    {
        AppointmentId = this.AppointmentId;
        ReasonCode = this.ReasonCode;
    }
}
