using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Billing.Domain;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (currencyCode.Length != 3)
            throw new ArgumentException("Currency code must be ISO-4217 (3 letters).", nameof(currencyCode));
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException("Currency mismatch.");
        return new Money(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException("Currency mismatch.");
        return new Money(Amount - other.Amount, CurrencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }

    public override string ToString() => $"{Amount:F2} {CurrencyCode}";
}
