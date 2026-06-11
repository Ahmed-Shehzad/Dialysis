using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Billing.Domain;

public enum ClaimStatus
{
    Assembled = 1,
    Submitted = 2,
    Acknowledged = 3,
    PartiallyPaid = 4,
    Paid = 5,
    Denied = 6,
    Cancelled = 7,
}

/// <summary>
/// Bundled set of charges submitted to a payer. Carries a clearinghouse control number (EDI 837 ISA/GS) for round-tripping with 835 remittance.
/// </summary>
public sealed class Claim : AggregateRoot<Guid>
{
    private readonly List<Guid> _chargeIds = new();
    private readonly List<ClaimAcknowledgement> _acknowledgements = new();

    private Claim()
    {
    }

    public Claim(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid PayerId { get; private set; }

    public string PayerCode { get; private set; } = string.Empty;

    public string ClaimFormatCode { get; private set; } = string.Empty;

    public Money BilledTotal { get; private set; } = null!;

    public string? ExternalControlNumber { get; private set; }

    public ClaimStatus Status { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public DateTime? AcknowledgedAtUtc { get; private set; }

    public string? PayerClaimControlNumber { get; private set; }

    /// <summary>
    /// Institutional (837I / UB-04) section — type of bill, statement period, admission,
    /// ICD-10-PCS procedures. Null on professional (837P) claims.
    /// </summary>
    public InstitutionalClaimDetails? Institutional { get; private set; }

    public IReadOnlyCollection<Guid> ChargeIds => _chargeIds;

    public IReadOnlyCollection<ClaimAcknowledgement> Acknowledgements => _acknowledgements;

    /// <summary>True when the format code denotes an institutional claim (X12 837I or paper UB-04).</summary>
    public static bool IsInstitutionalFormat(string claimFormatCode) =>
        string.Equals(claimFormatCode?.Trim(), EhrClaimFormats.Edi837Institutional, StringComparison.OrdinalIgnoreCase)
        || string.Equals(claimFormatCode?.Trim(), EhrClaimFormats.Ub04, StringComparison.OrdinalIgnoreCase);

    public static Claim Assemble(
        Guid id,
        Guid patientId,
        Guid payerId,
        string payerCode,
        string claimFormatCode,
        IReadOnlyList<Charge> charges,
        InstitutionalClaimDetails? institutional = null)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        if (payerId == Guid.Empty)
            throw new ArgumentException("Payer required.", nameof(payerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(payerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimFormatCode);
        if (charges is null || charges.Count == 0)
            throw new ArgumentException("Claim must include at least one charge.", nameof(charges));
        if (charges.Any(c => c.Status != ChargeStatus.Captured))
            throw new InvalidOperationException("All charges must be in Captured status to assemble.");

        var currency = charges[0].BilledAmount.CurrencyCode;
        if (charges.Any(c => c.BilledAmount.CurrencyCode != currency))
            throw new InvalidOperationException("Cannot mix currencies on a single claim.");
        var total = charges.Select(c => c.BilledAmount.Amount).Sum();

        // Institutional (837I / UB-04) claims carry the UB-04 section and bill every line
        // under a revenue code; professional (837P) claims never carry the section.
        var institutionalFormat = IsInstitutionalFormat(claimFormatCode);
        if (institutionalFormat && institutional is null)
            throw new InvalidOperationException(
                $"Claim format '{claimFormatCode.Trim()}' requires institutional details (type of bill, statement period).");
        if (!institutionalFormat && institutional is not null)
            throw new InvalidOperationException(
                $"Institutional details are only valid on institutional claim formats, not '{claimFormatCode.Trim()}'.");
        if (institutionalFormat && charges.Any(c => string.IsNullOrWhiteSpace(c.RevenueCode)))
            throw new InvalidOperationException("Every charge on an institutional claim must carry a revenue code.");

        var claim = new Claim(id)
        {
            PatientId = patientId,
            PayerId = payerId,
            PayerCode = payerCode.Trim().ToUpperInvariant(),
            ClaimFormatCode = claimFormatCode.Trim(),
            BilledTotal = new Money(total, currency),
            Status = ClaimStatus.Assembled,
            Institutional = institutional,
        };
        claim._chargeIds.AddRange(charges.Select(c => c.Id));
        foreach (var c in charges)
            c.AssignToClaim(id);

        return claim;
    }

    public void Submit(string externalControlNumber, DateTime submittedAtUtc)
    {
        if (Status != ClaimStatus.Assembled)
            throw new InvalidOperationException($"Cannot submit a claim in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(externalControlNumber);
        ExternalControlNumber = externalControlNumber.Trim();
        Status = ClaimStatus.Submitted;
        SubmittedAtUtc = submittedAtUtc;

        RaiseIntegrationEvent(new ClaimSubmittedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            ClaimId: Id,
            PatientId: PatientId,
            PayerId: PayerId,
            PayerCode: PayerCode,
            ClaimFormatCode: ClaimFormatCode,
            BilledTotal: BilledTotal.Amount,
            CurrencyCode: BilledTotal.CurrencyCode,
            ExternalControlNumber: ExternalControlNumber));
    }

    public void Acknowledge() { if (Status == ClaimStatus.Submitted) Status = ClaimStatus.Acknowledged; }

    public void MarkPaid() => Status = ClaimStatus.Paid;

    public void MarkPartiallyPaid() => Status = ClaimStatus.PartiallyPaid;

    public void MarkDenied() => Status = ClaimStatus.Denied;

    /// <summary>
    /// Records a parsed clearinghouse / payer acknowledgement. The 999 reports syntactic
    /// acceptance at the clearinghouse; the 277CA reports payer-level routing acceptance.
    /// We retain every ack so the operator audit trail shows the full transmission story.
    /// </summary>
    public void RecordAcknowledgement(ClaimAcknowledgement ack)
    {
        ArgumentNullException.ThrowIfNull(ack);
        _acknowledgements.Add(ack);
        if (Status == ClaimStatus.Cancelled)
            return;

        switch (ack.Verdict)
        {
            case ClaimAckVerdict.Accepted:
                if (!AcknowledgedAtUtc.HasValue || ack.ReceivedAtUtc > AcknowledgedAtUtc.Value)
                    AcknowledgedAtUtc = ack.ReceivedAtUtc;
                if (Status == ClaimStatus.Submitted)
                    Status = ClaimStatus.Acknowledged;
                if (!string.IsNullOrWhiteSpace(ack.PayerClaimControlNumber))
                    PayerClaimControlNumber = ack.PayerClaimControlNumber;
                break;
            case ClaimAckVerdict.Rejected:
                Status = ClaimStatus.Denied;
                break;
            case ClaimAckVerdict.AcceptedWithWarnings:
                if (Status == ClaimStatus.Submitted)
                    Status = ClaimStatus.Acknowledged;
                if (!AcknowledgedAtUtc.HasValue || ack.ReceivedAtUtc > AcknowledgedAtUtc.Value)
                    AcknowledgedAtUtc = ack.ReceivedAtUtc;
                break;
        }
    }
}

/// <summary>
/// One acknowledgement received against a claim. The kind distinguishes syntactic
/// (clearinghouse-level 999) from payer-level (277CA). Reason codes are surfaced
/// verbatim so the operator UI can show the clearinghouse / payer wording without
/// us having to maintain a code-to-string dictionary.
/// </summary>
public sealed class ClaimAcknowledgement
{
    public ClaimAcknowledgement(
        Guid id,
        ClaimAckKind kind,
        ClaimAckVerdict verdict,
        string? payerClaimControlNumber,
        IReadOnlyList<string> reasonCodes,
        DateTime receivedAtUtc)
    {
        Id = id;
        Kind = kind;
        Verdict = verdict;
        PayerClaimControlNumber = payerClaimControlNumber;
        ReasonCodes = reasonCodes;
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid Id { get; private set; }
    public ClaimAckKind Kind { get; private set; }
    public ClaimAckVerdict Verdict { get; private set; }
    public string? PayerClaimControlNumber { get; private set; }
    public IReadOnlyList<string> ReasonCodes { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
}

public enum ClaimAckKind
{
    FunctionalAck999 = 0,
    ClaimAck277Ca = 1,
}

public enum ClaimAckVerdict
{
    Accepted = 0,
    AcceptedWithWarnings = 1,
    Rejected = 2,
}
