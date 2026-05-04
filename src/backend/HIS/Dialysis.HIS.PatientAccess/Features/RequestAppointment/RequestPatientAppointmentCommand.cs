using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientAccess.Features.RequestAppointment;

/// <summary>Limited patient-initiated workflow (stub: records intent only).</summary>
public sealed record RequestPatientAppointmentCommand(Guid PatientId, string Notes)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PortalRead;
}
