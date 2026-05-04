using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;

public sealed record RegisterSpecialistEncounterCommand(
    Guid PatientId,
    string SpecialtyCode,
    string ExternalSystemCode,
    string Summary,
    DateTime? RecordedAtUtc = null)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
