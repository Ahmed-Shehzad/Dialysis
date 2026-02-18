namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// IEEE 11073 containment tree level: MDS → VMD → Channel → Metric.
/// The dotted OBX-4 sub-ID encodes the path through the containment tree.
/// </summary>
public readonly record struct ContainmentLevel
{
    public string Value { get; }

    public ContainmentLevel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ContainmentLevel Mds = new("MDS");
    public static readonly ContainmentLevel Vmd = new("VMD");
    public static readonly ContainmentLevel Channel = new("CHAN");
    public static readonly ContainmentLevel Metric = new("METRIC");

    public override string ToString() => Value;
    public static implicit operator string(ContainmentLevel v) => v.Value;
    public static explicit operator ContainmentLevel(string v) => new(v);
}
