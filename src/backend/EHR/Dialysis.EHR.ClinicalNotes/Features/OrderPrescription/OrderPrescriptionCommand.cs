using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;

public sealed record OrderPrescriptionCommand(
    Guid PatientId,
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
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PrescriptionOrder;
}
