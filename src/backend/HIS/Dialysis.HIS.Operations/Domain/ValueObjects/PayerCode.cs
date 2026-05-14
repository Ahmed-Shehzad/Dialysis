using System.Text.RegularExpressions;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Operations.Domain.ValueObjects;

/// <summary>
/// Identifier of a payer (insurer / contract counterparty) within HIS billing-export workflows.
/// 2–16 uppercase alphanumerics and hyphens. Wraps the wire-format <see cref="Value"/> string.
/// </summary>
public sealed partial class PayerCode : ValueObject
{
    [GeneratedRegex("^[A-Z0-9-]{2,16}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string Value { get; }

    public PayerCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("PayerCode is required.");
        var normalized = value.Trim();
        if (!Pattern().IsMatch(normalized))
            throw new DomainException($"PayerCode must match ^[A-Z0-9-]{{2,16}}$ (got '{value}').");
        Value = normalized;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
