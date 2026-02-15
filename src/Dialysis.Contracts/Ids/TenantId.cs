using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dialysis.Contracts.Ids;

/// <summary>
/// Strongly-typed identifier for a tenant in a multi-tenant setup.
/// Replaces primitive obsession over raw string Tenant IDs.
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly struct TenantId : IEquatable<TenantId>
{
    private static readonly Regex TenantPattern = new(@"^[A-Za-z0-9\-_]{1,64}$", RegexOptions.Compiled);

    public string Value { get; }

    private TenantId(string value) => Value = value;

    public static TenantId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tenant ID cannot be null or whitespace.", nameof(value));
        if (!TenantPattern.IsMatch(value))
            throw new ArgumentException($"Invalid Tenant ID format: {value}", nameof(value));
        return new TenantId(value);
    }

    public static TenantId? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !TenantPattern.IsMatch(value))
            return null;
        return new TenantId(value);
    }

    public bool Equals(TenantId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is TenantId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(TenantId left, TenantId right) => left.Equals(right);
    public static bool operator !=(TenantId left, TenantId right) => !left.Equals(right);

    public static implicit operator string(TenantId id) => id.Value ?? string.Empty;
}
