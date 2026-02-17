namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Unit of measure for numeric observations. Avoids primitive obsession.
/// </summary>
public sealed record UnitOfMeasure
{
    public string Value { get; }
    public string? System { get; }

    public UnitOfMeasure(string value, string? system = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 32)
            throw new ArgumentException("Unit must not exceed 32 characters.", nameof(value));
        Value = value.Trim();
        System = system?.Trim();
    }

    public static readonly UnitOfMeasure MillimetersOfMercury = new("mmHg", "http://unitsofmeasure.org");
    public static readonly UnitOfMeasure BeatsPerMinute = new("/min", "http://unitsofmeasure.org");
    public static readonly UnitOfMeasure Kilograms = new("kg", "http://unitsofmeasure.org");

    public override string ToString() => Value;
}
