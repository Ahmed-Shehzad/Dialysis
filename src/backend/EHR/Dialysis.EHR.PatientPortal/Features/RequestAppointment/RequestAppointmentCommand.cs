using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.RequestAppointment;

public sealed record RequestAppointmentCommand(
    Guid PatientId,
    string ReasonText,
    DateTime EarliestPreferredUtc,
    DateTime LatestPreferredUtc)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PortalAppointmentRequest;
}
