using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Billing.Domain;

public enum AdjudicationStatus
{
    Paid = 1,
    PartiallyPaid = 2,
    Denied = 3,
    Pending = 4,
}

/// <summary>835 remittance advice describing how a payer adjudicated a previously submitted claim.</summary>
public sealed class Remittance : AggregateRoot<Guid>
{
    private Remittance()
    {
    }

    public Remittance(Guid id) : base(id)
    {
    }

    public Guid ClaimId { get; private set; }

    public string PayerCode { get; private set; } = string.Empty;

    public Money PaidAmount { get; private set; } = default!;

    public Money AdjustmentAmount { get; private set; } = default!;

    public AdjudicationStatus AdjudicationStatus { get; private set; }

    public DateTime ReceivedAtUtc { get; private set; }

    public static Remittance Record(
        Guid id,
        Guid claimId,
        string payerCode,
        Money paidAmount,
        Money adjustmentAmount,
        AdjudicationStatus adjudicationStatus,
        DateTime receivedAtUtc)
    {
        if (claimId == Guid.Empty) throw new ArgumentException("Claim required.", nameof(claimId));
        ArgumentException.ThrowIfNullOrWhiteSpace(payerCode);
        ArgumentNullException.ThrowIfNull(paidAmount);
        ArgumentNullException.ThrowIfNull(adjustmentAmount);
        if (paidAmount.CurrencyCode != adjustmentAmount.CurrencyCode)
            throw new InvalidOperationException("Paid and adjustment amounts must share a currency.");

        var remittance = new Remittance(id)
        {
            ClaimId = claimId,
            PayerCode = payerCode.Trim().ToUpperInvariant(),
            PaidAmount = paidAmount,
            AdjustmentAmount = adjustmentAmount,
            AdjudicationStatus = adjudicationStatus,
            ReceivedAtUtc = receivedAtUtc,
        };

        remittance.RaiseIntegrationEvent(new RemittanceReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            RemittanceId: id,
            ClaimId: claimId,
            PaidAmount: paidAmount.Amount,
            AdjustmentAmount: adjustmentAmount.Amount,
            CurrencyCode: paidAmount.CurrencyCode,
            PayerCode: remittance.PayerCode,
            AdjudicationStatusCode: adjudicationStatus.ToString()));

        return remittance;
    }
}
