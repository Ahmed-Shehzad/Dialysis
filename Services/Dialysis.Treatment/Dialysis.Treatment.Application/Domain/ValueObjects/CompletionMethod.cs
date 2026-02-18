namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_12 – Treatment completion method.
/// </summary>
public readonly record struct CompletionMethod
{
    public string Value { get; }

    public CompletionMethod(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly CompletionMethod ClockTime = new("CT");
    public static readonly CompletionMethod ActualTreatmentTime = new("AT");
    public static readonly CompletionMethod UfRemoved = new("UF");
    public static readonly CompletionMethod KtV = new("KTV");
    public static readonly CompletionMethod User = new("USER");

    public override string ToString() => Value;
    public static implicit operator string(CompletionMethod v) => v.Value;
    public static explicit operator CompletionMethod(string v) => new(v);
}
