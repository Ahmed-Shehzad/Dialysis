using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed record BookAppointmentCommand(
    Guid PatientId,
    Guid ResourceId,
    DateTime StartUtc,
    DateTime EndUtc,
    string ResourceKindCode)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.AppointmentBook;
}
