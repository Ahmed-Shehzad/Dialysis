using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Integration.Domain;

/// <summary>Outbound EDI 837 claim transmission to a payer/clearinghouse, awaiting 835 remittance round-trip.</summary>
public sealed class InsurerTransmission : AggregateRoot<Guid>
{
    private InsurerTransmission()
    {
    }

    public InsurerTransmission(Guid id) : base(id)
    {
    }

    public Guid ClaimId { get; private set; }

    public string PayerCode { get; private set; } = string.Empty;

    public string ClaimFormatCode { get; private set; } = string.Empty;

    public string ExternalControlNumber { get; private set; } = string.Empty;

    public string PayloadDigest { get; private set; } = string.Empty;

    public OutboundTransmissionStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? LastAttemptedAtUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public static InsurerTransmission Queue(
        Guid id,
        Guid claimId,
        string payerCode,
        string claimFormatCode,
        string externalControlNumber,
        string payloadDigest)
    {
        if (claimId == Guid.Empty) throw new ArgumentException("Claim required.", nameof(claimId));
        ArgumentException.ThrowIfNullOrWhiteSpace(payerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimFormatCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalControlNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadDigest);

        return new InsurerTransmission(id)
        {
            ClaimId = claimId,
            PayerCode = payerCode.Trim().ToUpperInvariant(),
            ClaimFormatCode = claimFormatCode.Trim(),
            ExternalControlNumber = externalControlNumber.Trim(),
            PayloadDigest = payloadDigest.Trim(),
            Status = OutboundTransmissionStatus.Queued,
        };
    }

    public void RecordSent(DateTime attemptedAtUtc)
    {
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
