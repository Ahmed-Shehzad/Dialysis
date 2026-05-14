using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;

public sealed record RecordMedicationDispensingCommand(Guid MedicationOrderId, string BarcodeToken)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
