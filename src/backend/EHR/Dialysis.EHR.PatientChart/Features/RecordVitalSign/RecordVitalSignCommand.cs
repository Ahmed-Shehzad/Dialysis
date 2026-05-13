using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordVitalSign;

public sealed record RecordVitalSignCommand(
    Guid PatientId,
    Guid? EncounterId,
    string LoincCode,
    string? Display,
    decimal Value,
    string UnitCode,
    DateTime ObservedAtUtc,
    Guid? RecordedByProviderId)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.VitalsRecord;
}
