using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;

public sealed record RecordMedicationDispensingCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordMedicationDispensingCommand(Guid MedicationOrderId, string BarcodeToken)
    {
        this.MedicationOrderId = MedicationOrderId;
        this.BarcodeToken = BarcodeToken;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid MedicationOrderId { get; init; }
    public string BarcodeToken { get; init; }
    public void Deconstruct(out Guid MedicationOrderId, out string BarcodeToken)
    {
        MedicationOrderId = this.MedicationOrderId;
        BarcodeToken = this.BarcodeToken;
    }
}
