namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>
/// UCUM (Unified Code for Units of Measure) parser, canonicalizer, and converter. UCUM is
/// purely algorithmic — no terminology server is required, only the bundled tables embedded
/// in <c>Fhir.Metrics</c>. Use for unit validation in FHIR <c>Quantity</c> bindings, for
/// converting between commensurable units, and for comparing readings recorded in different
/// units (e.g. 70 kg vs 70 000 g).
/// </summary>
public interface IUcumService
{
    /// <summary>Validates that <paramref name="unitExpression"/> is a syntactically valid UCUM expression.</summary>
    bool TryParseUnit(string unitExpression);

    /// <summary>Reduces a quantity to canonical SI base units (e.g. <c>70 kg</c> → <c>70000 g</c>).</summary>
    bool TryCanonicalize(decimal value, string unit, out CanonicalQuantity canonical);

    /// <summary>
    /// Converts <paramref name="value"/> from <paramref name="fromUnit"/> to <paramref name="toUnit"/>.
    /// Fails when the two units are not commensurable (e.g. <c>kg</c> → <c>s</c>).
    /// </summary>
    bool TryConvert(decimal value, string fromUnit, string toUnit, out decimal converted);

    /// <summary>
    /// Compares two quantities of commensurable units. Returns <c>-1</c> when the first is less,
    /// <c>0</c> when equal, <c>1</c> when greater. Returns <c>false</c> when the units are not commensurable.
    /// </summary>
    bool TryCompare(decimal value1, string unit1, decimal value2, string unit2, out int comparison);
}

/// <summary>
/// Result of <see cref="IUcumService.TryCanonicalize"/>. <see cref="CanonicalUnit"/> is the SI base-unit
/// expression (e.g. <c>g</c> for masses, <c>s</c> for times). Use <see cref="CanonicalUnit"/> equality to
/// determine commensurability — two quantities can only be compared/added when their canonical units match.
/// </summary>
public sealed record CanonicalQuantity(decimal Value, string CanonicalUnit);
