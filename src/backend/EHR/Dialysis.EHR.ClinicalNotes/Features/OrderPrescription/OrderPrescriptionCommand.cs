using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;

public sealed record OrderPrescriptionCommand : ICommand<Guid>, IPermissionedCommand
{
    public OrderPrescriptionCommand(Guid PatientId,
        Guid EncounterId,
        Guid PrescribingProviderId,
        string MedicationRxnormCode,
        string MedicationDisplay,
        string DoseText,
        string FrequencyText,
        int QuantityDispensed,
        int RefillsAuthorized,
        string PharmacyNcpdpId,
        string TransmissionFormat = EhrPrescriptionFormats.NcpdpScriptNewRx)
    {
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.PrescribingProviderId = PrescribingProviderId;
        this.MedicationRxnormCode = MedicationRxnormCode;
        this.MedicationDisplay = MedicationDisplay;
        this.DoseText = DoseText;
        this.FrequencyText = FrequencyText;
        this.QuantityDispensed = QuantityDispensed;
        this.RefillsAuthorized = RefillsAuthorized;
        this.PharmacyNcpdpId = PharmacyNcpdpId;
        this.TransmissionFormat = TransmissionFormat;
    }
    public string RequiredPermission => EhrPermissions.PrescriptionOrder;
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public Guid PrescribingProviderId { get; init; }
    public string MedicationRxnormCode { get; init; }
    public string MedicationDisplay { get; init; }
    public string DoseText { get; init; }
    public string FrequencyText { get; init; }
    public int QuantityDispensed { get; init; }
    public int RefillsAuthorized { get; init; }
    public string PharmacyNcpdpId { get; init; }
    public string TransmissionFormat { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid EncounterId, out Guid PrescribingProviderId, out string MedicationRxnormCode, out string MedicationDisplay, out string DoseText, out string FrequencyText, out int QuantityDispensed, out int RefillsAuthorized, out string PharmacyNcpdpId, out string TransmissionFormat)
    {
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        PrescribingProviderId = this.PrescribingProviderId;
        MedicationRxnormCode = this.MedicationRxnormCode;
        MedicationDisplay = this.MedicationDisplay;
        DoseText = this.DoseText;
        FrequencyText = this.FrequencyText;
        QuantityDispensed = this.QuantityDispensed;
        RefillsAuthorized = this.RefillsAuthorized;
        PharmacyNcpdpId = this.PharmacyNcpdpId;
        TransmissionFormat = this.TransmissionFormat;
    }
}
