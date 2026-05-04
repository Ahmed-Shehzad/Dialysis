using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Medication.Features.RecordMedicationAdministration;

public sealed record RecordMedicationAdministrationCommand(Guid OrderId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.MedicationAdminRecord;
}

