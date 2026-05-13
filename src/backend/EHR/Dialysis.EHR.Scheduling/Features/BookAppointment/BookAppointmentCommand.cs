using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.BookAppointment;

public sealed record BookAppointmentCommand(
    Guid PatientId,
    Guid ProviderId,
    DateTime StartUtc,
    DateTime EndUtc,
    string EncounterClassCode,
    string? VisitReason)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.AppointmentBook;
}
