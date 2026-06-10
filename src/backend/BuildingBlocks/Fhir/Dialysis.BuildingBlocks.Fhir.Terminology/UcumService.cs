using System.Globalization;
using Fhir.Metrics;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>
/// UCUM implementation backed by <c>Fhir.Metrics</c>. The <see cref="SystemOfUnits"/> instance is
/// lazily loaded once and shared across all calls — the bundled UCUM tables are large and immutable.
///
/// Conversion uses the canonical-ratio strategy: both source and target units are reduced to their
/// canonical SI representation; conversion is the ratio of the source-canonical value to the
/// target-unit-canonical value. This works for every commensurable pair without depending on
/// <c>Fhir.Metrics</c>'s direct-conversion path (which is unimplemented for non-canonical metrics).
/// </summary>
public sealed class UcumService : IUcumService
{
    private static readonly Lazy<SystemOfUnits> _system = new(UCUM.Load, isThreadSafe: true);

    private static SystemOfUnits Units => _system.Value;

    public bool TryParseUnit(string unitExpression)
    {
        if (string.IsNullOrWhiteSpace(unitExpression))
            return false;
        try
        {
            _ = Units.Metric(unitExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryCanonicalize(decimal value, string unit, out CanonicalQuantity canonical)
    {
        canonical = default!;
        if (string.IsNullOrWhiteSpace(unit))
            return false;
        try
        {
            var quantity = Units.Quantity(value.ToString(CultureInfo.InvariantCulture), unit);
            var canon = Units.Canonical(quantity);
            canonical = new CanonicalQuantity(canon.Value.ToDecimal(), canon.Metric.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryConvert(decimal value, string fromUnit, string toUnit, out decimal converted)
    {
        converted = default;
        if (!TryCanonicalize(value, fromUnit, out var sourceCanonical))
            return false;
        if (!TryCanonicalize(1m, toUnit, out var targetCanonical))
            return false;
        if (sourceCanonical.CanonicalUnit != targetCanonical.CanonicalUnit)
            return false;
        if (targetCanonical.Value == 0m)
            return false;
        converted = sourceCanonical.Value / targetCanonical.Value;
        return true;
    }

    public bool TryCompare(decimal value1, string unit1, decimal value2, string unit2, out int comparison)
    {
        comparison = 0;
        if (!TryCanonicalize(value1, unit1, out var a))
            return false;
        if (!TryCanonicalize(value2, unit2, out var b))
            return false;
        if (a.CanonicalUnit != b.CanonicalUnit)
            return false;
        comparison = a.Value.CompareTo(b.Value);
        return true;
    }
}
