namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a tenant. Avoids primitive obsession.
/// </summary>
public sealed record TenantId
{
    public const string Default = "default";

    public string Value { get; }

    public TenantId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 64)
            throw new ArgumentException("TenantId must not exceed 64 characters.", nameof(value));
        Value = value.Trim();
    }

    public static TenantId DefaultTenant => new(Default);

    public override string ToString() => Value;
    public static implicit operator string(TenantId id) => id.Value;
}
