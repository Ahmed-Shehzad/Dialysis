using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Integration.Domain;

/// <summary>
/// Outbound NCPDP SCRIPT transmission for a prescription. The original prescription lives in the ClinicalNotes BC;
/// this aggregate tracks the wire-level handshake (queued → sent → acknowledged/rejected) with the destination pharmacy.
/// </summary>
public sealed class PharmacyTransmission : AggregateRoot<Guid>
{
    private PharmacyTransmission()
    {
    }

    public PharmacyTransmission(Guid id) : base(id)
    {
    }

    public Guid PrescriptionId { get; private set; }

    public string PharmacyNcpdpId { get; private set; } = string.Empty;

    public string TransmissionFormat { get; private set; } = string.Empty;

    public string PayloadDigest { get; private set; } = string.Empty;

    public string? ExternalControlNumber { get; private set; }

    public OutboundTransmissionStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? LastAttemptedAtUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public static PharmacyTransmission Queue(
        Guid id,
        Guid prescriptionId,
        string pharmacyNcpdpId,
        string transmissionFormat,
        string payloadDigest)
    {
        if (prescriptionId == Guid.Empty) throw new ArgumentException("Prescription required.", nameof(prescriptionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(pharmacyNcpdpId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transmissionFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadDigest);

        return new PharmacyTransmission(id)
        {
            PrescriptionId = prescriptionId,
            PharmacyNcpdpId = pharmacyNcpdpId.Trim(),
            TransmissionFormat = transmissionFormat.Trim(),
            PayloadDigest = payloadDigest.Trim(),
            Status = OutboundTransmissionStatus.Queued,
        };
    }

    public void RecordSent(string externalControlNumber, DateTime attemptedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalControlNumber);
        ExternalControlNumber = externalControlNumber.Trim();
        Status = OutboundTransmissionStatus.Sent;
        AttemptCount++;
        LastAttemptedAtUtc = attemptedAtUtc;
        LastErrorCode = null;
    }

    public void RecordAcknowledged() => Status = OutboundTransmissionStatus.Acknowledged;

    public void RecordRejected(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        Status = OutboundTransmissionStatus.Rejected;
        LastErrorCode = errorCode.Trim();
    }

    public void RecordFailure(string errorCode, DateTime attemptedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        AttemptCount++;
        LastAttemptedAtUtc = attemptedAtUtc;
        LastErrorCode = errorCode.Trim();
        Status = OutboundTransmissionStatus.Failed;
    }
}
