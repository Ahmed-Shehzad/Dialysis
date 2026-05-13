using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordImmunization;

public sealed record RecordImmunizationCommand(
    Guid PatientId,
    string CvxCode,
    string? CvxDisplay,
    DateOnly AdministeredOn,
    string? LotNumber,
    string? Manufacturer,
    string? SiteCode,
    Guid? AdministeringProviderId)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ImmunizationRecord;
}
