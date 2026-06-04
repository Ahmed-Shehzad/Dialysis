using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed record PlaceMedicationOrderCommand : ICommand<Guid>, IPermissionedCommand
{
    public PlaceMedicationOrderCommand(Guid PatientId,
        string DrugCode,
        string Dosage)
    {
        this.PatientId = PatientId;
        this.DrugCode = DrugCode;
        this.Dosage = Dosage;
    }
    public string RequiredPermission => HisPermissions.MedicationOrderPlace;
    public Guid PatientId { get; init; }
    public string DrugCode { get; init; }
    public string Dosage { get; init; }
    public void Deconstruct(out Guid PatientId, out string DrugCode, out string Dosage)
    {
        PatientId = this.PatientId;
        DrugCode = this.DrugCode;
        Dosage = this.Dosage;
    }
}
