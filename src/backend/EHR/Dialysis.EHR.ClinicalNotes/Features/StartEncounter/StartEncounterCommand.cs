using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.StartEncounter;

public sealed record StartEncounterCommand(
    Guid PatientId,
    Guid ProviderId,
    string EncounterClassCode,
    Guid? AppointmentId)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.EncounterStart;
}
