namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_13 – Ultrafiltration mode.
/// </summary>
public readonly record struct UltrafiltrationMode
{
    public string Value { get; }

    public UltrafiltrationMode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly UltrafiltrationMode None = new("NONE");
    public static readonly UltrafiltrationMode ConstantWithTarget = new("CONST-WT");
    public static readonly UltrafiltrationMode ProfileWithTarget = new("PRO-WT");
    public static readonly UltrafiltrationMode ConstantWithoutTarget = new("CONST-WOT");
    public static readonly UltrafiltrationMode ProfileWithoutTarget = new("PRO-WOT");

    public override string ToString() => Value;
    public static implicit operator string(UltrafiltrationMode v) => v.Value;
    public static explicit operator UltrafiltrationMode(string v) => new(v);
}
