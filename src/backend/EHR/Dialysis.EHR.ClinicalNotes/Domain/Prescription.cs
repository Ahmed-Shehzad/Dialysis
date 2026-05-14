using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.ClinicalNotes.Domain;

public enum PrescriptionStatus
{
    Active = 1,
    OnHold = 2,
    Cancelled = 3,
    Completed = 4,
    AcknowledgedByPharmacy = 5,
}

/// <summary>
/// Medication order issued during an encounter. The <see cref="Integration"/> bounded context
/// is responsible for transmitting it via NCPDP SCRIPT to the pharmacy and observing the ACK.
/// </summary>
public sealed class Prescription : AggregateRoot<Guid>
{
    private Prescription()
    {
    }

    public Prescription(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid EncounterId { get; private set; }

    public Guid PrescribingProviderId { get; private set; }

    public string MedicationRxnormCode { get; private set; } = string.Empty;

    public string MedicationDisplay { get; private set; } = string.Empty;

    public string DoseText { get; private set; } = string.Empty;

    public string FrequencyText { get; private set; } = string.Empty;

    public int QuantityDispensed { get; private set; }

    public int RefillsAuthorized { get; private set; }

    public string PharmacyNcpdpId { get; private set; } = string.Empty;

    public string TransmissionFormat { get; private set; } = string.Empty;

    public PrescriptionStatus Status { get; private set; }

    public string? CancellationReasonCode { get; private set; }

    public static Prescription Order(
        Guid id,
        Guid patientId,
        Guid encounterId,
        Guid prescribingProviderId,
        string medicationRxnormCode,
        string medicationDisplay,
        string doseText,
        string frequencyText,
        int quantityDispensed,
        int refillsAuthorized,
        string pharmacyNcpdpId,
        string transmissionFormat)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        if (encounterId == Guid.Empty) throw new ArgumentException("Encounter required.", nameof(encounterId));
        if (prescribingProviderId == Guid.Empty) throw new ArgumentException("Prescriber required.", nameof(prescribingProviderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(medicationRxnormCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(medicationDisplay);
        ArgumentException.ThrowIfNullOrWhiteSpace(doseText);
        ArgumentException.ThrowIfNullOrWhiteSpace(frequencyText);
        ArgumentException.ThrowIfNullOrWhiteSpace(pharmacyNcpdpId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transmissionFormat);
        if (quantityDispensed <= 0) throw new ArgumentOutOfRangeException(nameof(quantityDispensed));
        if (refillsAuthorized < 0) throw new ArgumentOutOfRangeException(nameof(refillsAuthorized));

        var rx = new Prescription(id)
        {
            PatientId = patientId,
            EncounterId = encounterId,
            PrescribingProviderId = prescribingProviderId,
            MedicationRxnormCode = medicationRxnormCode.Trim(),
            MedicationDisplay = medicationDisplay.Trim(),
            DoseText = doseText.Trim(),
            FrequencyText = frequencyText.Trim(),
            QuantityDispensed = quantityDispensed,
            RefillsAuthorized = refillsAuthorized,
            PharmacyNcpdpId = pharmacyNcpdpId.Trim(),
            TransmissionFormat = transmissionFormat.Trim(),
            Status = PrescriptionStatus.Active,
        };

        rx.RaiseIntegrationEvent(new PrescriptionOrderedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PrescriptionId: id,
            PatientId: patientId,
            EncounterId: encounterId,
            PrescribingProviderId: prescribingProviderId,
            MedicationRxnormCode: rx.MedicationRxnormCode,
            MedicationDisplay: rx.MedicationDisplay,
            DoseText: rx.DoseText,
            FrequencyText: rx.FrequencyText,
            QuantityDispensed: quantityDispensed,
            RefillsAuthorized: refillsAuthorized,
            PharmacyNcpdpId: rx.PharmacyNcpdpId,
            TransmissionFormat: rx.TransmissionFormat));

        return rx;
    }

    public void Cancel(string reasonCode)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel a prescription in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        Status = PrescriptionStatus.Cancelled;
        CancellationReasonCode = reasonCode.Trim();

        RaiseIntegrationEvent(new PrescriptionCancelledIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PrescriptionId: Id,
            PatientId: PatientId,
            ReasonCode: CancellationReasonCode));
    }

    public void RecordPharmacyAcknowledgement()
    {
        if (Status != PrescriptionStatus.Active) return;
        Status = PrescriptionStatus.AcknowledgedByPharmacy;
    }
}
