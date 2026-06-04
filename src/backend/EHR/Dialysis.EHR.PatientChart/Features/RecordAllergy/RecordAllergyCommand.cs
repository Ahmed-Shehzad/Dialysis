using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordAllergy;

/// <summary>
/// Allergy recording — clinically critical write. Opted into the durable command bus
/// the same way as <c>RecordVitalSignCommand</c>; flag
/// <c>Ehr:DurableCommands:RecordAllergy:Enabled</c> controls whether the controller
/// returns 202 or stays on the synchronous path.
/// </summary>
[DurableCommand("ehr")]
public sealed record RecordAllergyCommand(
    Guid PatientId,
    string AllergenSystem,
    string AllergenCode,
    string? AllergenDisplay,
    AllergySeverity Severity,
    AllergyVerificationStatus VerificationStatus,
    string? ReactionText,
    DateOnly? OnsetDate,
    Guid AllergyId = default)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.AllergyRecord;
}
