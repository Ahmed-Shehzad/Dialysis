using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed record BookAppointmentCommand(
    Guid PatientId,
    Guid ProviderId,
    DateTime SlotStartUtc,
    DateTime SlotEndUtc) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.SchedulingBook;
}
