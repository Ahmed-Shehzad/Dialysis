using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed record PlaceMedicationOrderCommand(Guid PatientId, string MedicationCode)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.MedicationOrderPlace;
}
