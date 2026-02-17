namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// A numeric value with its unit for observations. Avoids primitive obsession.
/// </summary>
public sealed record NumericObservationValue
{
    public decimal Value { get; }
    public UnitOfMeasure Unit { get; }
    public string Display { get; }

    public NumericObservationValue(decimal value, UnitOfMeasure unit, string? display = null)
    {
        Value = value;
        Unit = unit;
        Display = display ?? $"{value} {unit.Value}";
    }
}
