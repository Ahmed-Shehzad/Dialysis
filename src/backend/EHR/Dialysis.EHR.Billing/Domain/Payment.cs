using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Billing.Domain;

public enum PaymentMethod
{
    Cash = 1,
    Check = 2,
    Card = 3,
    Ach = 4,
    PayerRemit = 5,
}

public sealed class Payment : AggregateRoot<Guid>
{
    private Payment()
    {
    }

    public Payment(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid? ClaimId { get; private set; }

    public Money Amount { get; private set; } = default!;

    public PaymentMethod Method { get; private set; }

    public DateTime PostedAtUtc { get; private set; }

    public string? ExternalReference { get; private set; }

    public static Payment Post(
        Guid id,
        Guid patientId,
        Guid? claimId,
        Money amount,
        PaymentMethod method,
        DateTime postedAtUtc,
        string? externalReference = null)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0) throw new ArgumentException("Payment amount must be positive.", nameof(amount));

        var payment = new Payment(id)
        {
            PatientId = patientId,
            ClaimId = claimId,
            Amount = amount,
            Method = method,
            PostedAtUtc = postedAtUtc,
            ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim(),
        };

        payment.RaiseIntegrationEvent(new PaymentPostedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            PaymentId: id,
            PatientId: patientId,
            ClaimId: claimId,
            Amount: amount.Amount,
            CurrencyCode: amount.CurrencyCode,
            PaymentMethodCode: method.ToString(),
            PostedAtUtc: postedAtUtc));

        return payment;
    }
}
