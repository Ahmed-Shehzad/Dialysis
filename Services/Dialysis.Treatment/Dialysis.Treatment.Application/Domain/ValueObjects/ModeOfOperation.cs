namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_01 – Dialysis machine mode of operation.
/// </summary>
public readonly record struct ModeOfOperation
{
    public string Value { get; }

    public ModeOfOperation(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ModeOfOperation PreTreatment = new("PRETX");
    public static readonly ModeOfOperation Treatment = new("TX");
    public static readonly ModeOfOperation PostTreatment = new("POSTTX");
    public static readonly ModeOfOperation Disinfection = new("DIS");
    public static readonly ModeOfOperation Idle = new("IDL");
    public static readonly ModeOfOperation Service = new("SVC");

    public override string ToString() => Value;
    public static implicit operator string(ModeOfOperation v) => v.Value;
    public static explicit operator ModeOfOperation(string v) => new(v);
}
