using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Integration.Translators;

/// <summary>
/// Anticorruption Layer (Evans pp. 258–260) at the ClinicalNotes-Prescription ↔ Integration-Pharmacy
/// boundary. Translates the cross-context <see cref="PrescriptionOrderedIntegrationEvent"/> into the
/// pharmacy-transmission-local <see cref="OutboundPharmacyOrder"/> intent. The consumer no longer touches
/// the event payload past this translation step.
/// </summary>
public static class PrescriptionOrderedTranslator
{
    public static OutboundPharmacyOrder Translate(PrescriptionOrderedIntegrationEvent message) =>
        new(
            PrescriptionId: message.PrescriptionId,
            PharmacyNcpdpId: message.PharmacyNcpdpId,
            TransmissionFormat: message.TransmissionFormat,
            PayloadDigest: ComputeDigest(message));

    private static string ComputeDigest(PrescriptionOrderedIntegrationEvent message) =>
        $"{message.PrescriptionId:N}|{message.MedicationRxnormCode}|{message.DoseText}|{message.FrequencyText}|{message.QuantityDispensed}";
}

/// <summary>
/// EHR Integration-local intent translated from a Clinical-Notes prescription-ordered event. Carries the
/// fields the pharmacy transmission needs to queue an outbound NCPDP SCRIPT message.
/// </summary>
public sealed record OutboundPharmacyOrder
{
    /// <summary>
    /// EHR Integration-local intent translated from a Clinical-Notes prescription-ordered event. Carries the
    /// fields the pharmacy transmission needs to queue an outbound NCPDP SCRIPT message.
    /// </summary>
    public OutboundPharmacyOrder(Guid PrescriptionId,
        string PharmacyNcpdpId,
        string TransmissionFormat,
        string PayloadDigest)
    {
        this.PrescriptionId = PrescriptionId;
        this.PharmacyNcpdpId = PharmacyNcpdpId;
        this.TransmissionFormat = TransmissionFormat;
        this.PayloadDigest = PayloadDigest;
    }
    public Guid PrescriptionId { get; init; }
    public string PharmacyNcpdpId { get; init; }
    public string TransmissionFormat { get; init; }
    public string PayloadDigest { get; init; }
    public void Deconstruct(out Guid PrescriptionId, out string PharmacyNcpdpId, out string TransmissionFormat, out string PayloadDigest)
    {
        PrescriptionId = this.PrescriptionId;
        PharmacyNcpdpId = this.PharmacyNcpdpId;
        TransmissionFormat = this.TransmissionFormat;
        PayloadDigest = this.PayloadDigest;
    }
}
