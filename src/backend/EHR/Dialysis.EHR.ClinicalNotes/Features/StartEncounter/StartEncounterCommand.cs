using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.StartEncounter;

public sealed record StartEncounterCommand : ICommand<Guid>, IPermissionedCommand
{
    public StartEncounterCommand(Guid PatientId,
        Guid ProviderId,
        string EncounterClassCode,
        Guid? AppointmentId)
    {
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.EncounterClassCode = EncounterClassCode;
        this.AppointmentId = AppointmentId;
    }
    public string RequiredPermission => EhrPermissions.EncounterStart;
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public string EncounterClassCode { get; init; }
    public Guid? AppointmentId { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid ProviderId, out string EncounterClassCode, out Guid? AppointmentId)
    {
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        EncounterClassCode = this.EncounterClassCode;
        AppointmentId = this.AppointmentId;
    }
}
