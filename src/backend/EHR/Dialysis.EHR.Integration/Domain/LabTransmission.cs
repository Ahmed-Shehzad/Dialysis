using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Integration.Domain;

/// <summary>Outbound HL7v2-ORM or FHIR ServiceRequest transmission for a lab order.</summary>
public sealed class LabTransmission : AggregateRoot<Guid>
{
    private LabTransmission()
    {
    }

    public LabTransmission(Guid id) : base(id)
    {
    }

    public Guid LabOrderId { get; private set; }

    public string LabFacilityCode { get; private set; } = string.Empty;

    public string TransmissionFormat { get; private set; } = string.Empty;

    public string PayloadDigest { get; private set; } = string.Empty;

    public string? ExternalControlNumber { get; private set; }

    public OutboundTransmissionStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? LastAttemptedAtUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public static LabTransmission Queue(
        Guid id,
        Guid labOrderId,
        string labFacilityCode,
        string transmissionFormat,
        string payloadDigest)
    {
        if (labOrderId == Guid.Empty) throw new ArgumentException("Lab order required.", nameof(labOrderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(labFacilityCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(transmissionFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadDigest);

        return new LabTransmission(id)
        {
            LabOrderId = labOrderId,
            LabFacilityCode = labFacilityCode.Trim(),
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
    }

    public void RecordAcknowledged() => Status = OutboundTransmissionStatus.Acknowledged;

    public void RecordFailure(string errorCode, DateTime attemptedAtUtc)
    {
        AttemptCount++;
        LastAttemptedAtUtc = attemptedAtUtc;
        LastErrorCode = errorCode;
        Status = OutboundTransmissionStatus.Failed;
    }
}
