using System.Text.RegularExpressions;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Medication.Domain.ValueObjects;

public sealed partial class DrugCode : ValueObject
{
    [GeneratedRegex("^[A-Z0-9-]{2,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string Value { get; }

    public DrugCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("DrugCode cannot be empty.");
        var trimmed = value.Trim();
        if (!Pattern().IsMatch(trimmed))
            throw new DomainException("DrugCode must match ^[A-Z0-9-]{2,32}$.");
        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
