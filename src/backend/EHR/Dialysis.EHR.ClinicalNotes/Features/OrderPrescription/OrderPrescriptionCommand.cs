using Dialysis.CQRS.Commands;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;

public sealed record OrderPrescriptionCommand : ICommand<OrderPlacementResult>, IPermissionedCommand
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
        string TransmissionFormat = EhrPrescriptionFormats.NcpdpScriptNewRx,
        bool AcknowledgeAdvisories = false,
        string? OverrideReason = null,
        string? OverriddenBy = null)
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
        this.AcknowledgeAdvisories = AcknowledgeAdvisories;
        this.OverrideReason = OverrideReason;
        this.OverriddenBy = OverriddenBy;
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

    /// <summary>When true, blocking safety advisories are overridden (requires <see cref="OverrideReason"/>).</summary>
    public bool AcknowledgeAdvisories { get; init; }

    /// <summary>The clinician's reason for overriding a blocking advisory; audited on the prescription.</summary>
    public string? OverrideReason { get; init; }

    /// <summary>Server-populated identity of the overriding clinician (from the authenticated principal).</summary>
    public string? OverriddenBy { get; init; }
}
