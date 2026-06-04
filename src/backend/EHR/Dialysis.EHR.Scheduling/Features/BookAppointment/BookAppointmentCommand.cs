using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Scheduling.Features.BookAppointment;

public sealed record BookAppointmentCommand : ICommand<Guid>, IPermissionedCommand
{
    public BookAppointmentCommand(Guid PatientId,
        Guid ProviderId,
        DateTime StartUtc,
        DateTime EndUtc,
        string EncounterClassCode,
        string? VisitReason)
    {
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.StartUtc = StartUtc;
        this.EndUtc = EndUtc;
        this.EncounterClassCode = EncounterClassCode;
        this.VisitReason = VisitReason;
    }
    public string RequiredPermission => EhrPermissions.AppointmentBook;
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public string EncounterClassCode { get; init; }
    public string? VisitReason { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid ProviderId, out DateTime StartUtc, out DateTime EndUtc, out string EncounterClassCode, out string? VisitReason)
    {
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        StartUtc = this.StartUtc;
        EndUtc = this.EndUtc;
        EncounterClassCode = this.EncounterClassCode;
        VisitReason = this.VisitReason;
    }
}
