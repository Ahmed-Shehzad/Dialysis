using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.RequestAppointment;

public sealed record RequestAppointmentCommand : ICommand<Guid>, IPermissionedCommand
{
    public RequestAppointmentCommand(Guid PatientId,
        string ReasonText,
        DateTime EarliestPreferredUtc,
        DateTime LatestPreferredUtc)
    {
        this.PatientId = PatientId;
        this.ReasonText = ReasonText;
        this.EarliestPreferredUtc = EarliestPreferredUtc;
        this.LatestPreferredUtc = LatestPreferredUtc;
    }
    public string RequiredPermission => EhrPermissions.PortalAppointmentRequest;
    public Guid PatientId { get; init; }
    public string ReasonText { get; init; }
    public DateTime EarliestPreferredUtc { get; init; }
    public DateTime LatestPreferredUtc { get; init; }
    public void Deconstruct(out Guid PatientId, out string ReasonText, out DateTime EarliestPreferredUtc, out DateTime LatestPreferredUtc)
    {
        PatientId = this.PatientId;
        ReasonText = this.ReasonText;
        EarliestPreferredUtc = this.EarliestPreferredUtc;
        LatestPreferredUtc = this.LatestPreferredUtc;
    }
}
