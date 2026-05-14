using System.Text.RegularExpressions;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.PatientFlow.Domain.ValueObjects;

public sealed partial class WardCode : ValueObject
{
    [GeneratedRegex("^[A-Z0-9-]{2,16}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string Value { get; }

    public WardCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("WardCode cannot be empty.");
        var trimmed = value.Trim();
        if (!Pattern().IsMatch(trimmed))
            throw new DomainException("WardCode must match ^[A-Z0-9-]{2,16}$.");
        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
