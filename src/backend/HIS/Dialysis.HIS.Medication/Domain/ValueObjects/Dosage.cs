using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Medication.Domain.ValueObjects;

public sealed class Dosage : ValueObject
{
    public string Value { get; }

    public Dosage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Dosage cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > 64)
            throw new DomainException("Dosage must be 64 chars or fewer.");
        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
