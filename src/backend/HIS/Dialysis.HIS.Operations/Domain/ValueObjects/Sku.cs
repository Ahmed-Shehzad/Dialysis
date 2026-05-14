using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Operations.Domain.ValueObjects;

/// <summary>
/// Stock-keeping unit identifier for an <see cref="InventoryItem"/>. Trimmed, 1–64 chars.
/// </summary>
public sealed class Sku : ValueObject
{
    public string Value { get; }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Sku is required.");
        var normalized = value.Trim();
        if (normalized.Length > 64)
            throw new DomainException("Sku must be 64 chars or fewer.");
        Value = normalized;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
