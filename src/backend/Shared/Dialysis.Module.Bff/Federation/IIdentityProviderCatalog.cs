using Dialysis.Module.Bff.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.Module.Bff.Federation;

/// <summary>
/// Read-only view of the upstream IdPs the BFF will broker through Keycloak. Backed by
/// <see cref="IdentityFederationOptions"/>; surfaced to the SPA via <c>GET {base}/identity/providers</c>.
/// </summary>
public interface IIdentityProviderCatalog
{
    /// <summary>Enabled providers in configuration order. Empty when federation is not configured.</summary>
    IReadOnlyList<IdentityProviderDescriptor> List();

    /// <summary>
    /// Returns <c>true</c> only when <paramref name="alias"/> matches an enabled entry. The login
    /// handler must call this before forwarding a caller-supplied alias to Keycloak — otherwise an
    /// attacker can probe arbitrary Keycloak broker aliases.
    /// </summary>
    bool IsKnown(string alias);
}

/// <summary>Public projection of an <see cref="IdentityProviderEntry"/>.</summary>
public sealed record IdentityProviderDescriptor
{
    /// <summary>Public projection of an <see cref="IdentityProviderEntry"/>.</summary>
    public IdentityProviderDescriptor(string Alias, string DisplayName, string? IconUri)
    {
        this.Alias = Alias;
        this.DisplayName = DisplayName;
        this.IconUri = IconUri;
    }
    public string Alias { get; init; }
    public string DisplayName { get; init; }
    public string? IconUri { get; init; }
    public void Deconstruct(out string alias, out string displayName, out string? iconUri)
    {
        alias = this.Alias;
        displayName = this.DisplayName;
        iconUri = this.IconUri;
    }
}

/// <summary>
/// Resolves the catalog from configuration. Uses <see cref="IOptionsMonitor{T}"/> so live config
/// reloads land without a host restart.
/// </summary>
public sealed class ConfiguredIdentityProviderCatalog : IIdentityProviderCatalog
{
    private readonly IOptionsMonitor<IdentityFederationOptions> _options;
    /// <summary>Resolves the catalog from configuration.</summary>
    public ConfiguredIdentityProviderCatalog(IOptionsMonitor<IdentityFederationOptions> options) => _options = options;

    public IReadOnlyList<IdentityProviderDescriptor> List() =>
        [.. _options.CurrentValue.Providers
            .Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Alias))
            .Select(p => new IdentityProviderDescriptor(
                p.Alias,
                string.IsNullOrWhiteSpace(p.DisplayName) ? p.Alias : p.DisplayName,
                string.IsNullOrWhiteSpace(p.IconUri) ? null : p.IconUri))];

    public bool IsKnown(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return false;
        foreach (var entry in _options.CurrentValue.Providers)
        {
            if (!entry.Enabled || string.IsNullOrWhiteSpace(entry.Alias))
                continue;
            if (string.Equals(entry.Alias, alias, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
