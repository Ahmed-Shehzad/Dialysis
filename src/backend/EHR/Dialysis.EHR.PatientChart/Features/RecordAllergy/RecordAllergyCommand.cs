using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordAllergy;

public sealed record RecordAllergyCommand(
    Guid PatientId,
    string AllergenSystem,
    string AllergenCode,
    string? AllergenDisplay,
    AllergySeverity Severity,
    AllergyVerificationStatus VerificationStatus,
    string? ReactionText,
    DateOnly? OnsetDate)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.AllergyRecord;
}
