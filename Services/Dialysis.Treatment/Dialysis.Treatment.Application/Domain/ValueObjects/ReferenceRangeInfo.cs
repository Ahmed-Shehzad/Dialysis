namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// Parsed OBX-7 reference range. Supports formats: <c>> lower</c>, <c>&lt; upper</c>, <c>lower-upper</c>.
/// </summary>
public sealed record ReferenceRangeInfo(
    double? Lower,
    double? Upper,
    ReferenceRangeKind Kind);

/// <summary>
/// Interpretation of the reference range.
/// </summary>
public enum ReferenceRangeKind
{
    /// <summary>Bounded range: value within lower-upper.</summary>
    Bounded,
    /// <summary>Only lower bound: value must be &gt; lower.</summary>
    GreaterThanLower,
    /// <summary>Only upper bound: value must be &lt; upper.</summary>
    LessThanUpper
}
