using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed record PlaceMedicationOrderCommand(
    Guid PatientId,
    string DrugCode,
    string Dosage) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.MedicationOrderPlace;
}
