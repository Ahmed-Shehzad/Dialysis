using Dialysis.DomainDrivenDesign.Primitives;
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

    public Money BilledTotal { get; private set; } = default!;

    public string? ExternalControlNumber { get; private set; }

    public ClaimStatus Status { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public IReadOnlyCollection<Guid> ChargeIds => _chargeIds;

    public static Claim Assemble(
        Guid id,
        Guid patientId,
        Guid payerId,
        string payerCode,
        string claimFormatCode,
        IReadOnlyList<Charge> charges)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        if (payerId == Guid.Empty) throw new ArgumentException("Payer required.", nameof(payerId));
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

        var claim = new Claim(id)
        {
            PatientId = patientId,
            PayerId = payerId,
            PayerCode = payerCode.Trim().ToUpperInvariant(),
            ClaimFormatCode = claimFormatCode.Trim(),
            BilledTotal = new Money(total, currency),
            Status = ClaimStatus.Assembled,
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
}
