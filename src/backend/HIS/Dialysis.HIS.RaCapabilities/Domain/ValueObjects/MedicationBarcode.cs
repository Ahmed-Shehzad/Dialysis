using System.Text.RegularExpressions;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.RaCapabilities.Domain.ValueObjects;

/// <summary>
/// Scanned barcode token captured at point-of-dispense. 6–32 uppercase alphanumerics.
/// </summary>
public sealed partial class MedicationBarcode : ValueObject
{
    [GeneratedRegex("^[A-Z0-9]{6,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string Value { get; }

    public MedicationBarcode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("MedicationBarcode is required.");
        var normalized = value.Trim();
        if (!Pattern().IsMatch(normalized))
            throw new DomainException($"MedicationBarcode must match ^[A-Z0-9]{{6,32}}$ (got '{value}').");
        Value = normalized;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
