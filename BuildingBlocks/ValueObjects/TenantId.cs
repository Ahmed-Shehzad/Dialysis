namespace BuildingBlocks.ValueObjects;

/// <summary>
/// Strongly-typed tenant identifier for multi-tenancy (C5).
/// </summary>
public readonly record struct TenantId
{
    public const string DefaultValue = "default";

    public static readonly TenantId Default = new(DefaultValue);

    public string Value { get; }

    public TenantId(string value)
    {
        Value = string.IsNullOrWhiteSpace(value) ? DefaultValue : value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(TenantId id) => id.Value;
    public static explicit operator TenantId(string value) => new(value);

    public static bool operator ==(TenantId left, string? right) => left.Value == (right ?? string.Empty);
    public static bool operator ==(string? left, TenantId right) => right == left;
    public static bool operator !=(TenantId left, string? right) => !(left == right);
    public static bool operator !=(string? left, TenantId right) => !(right == left);
}
