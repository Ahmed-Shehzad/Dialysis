using Dialysis.Identity.Bff.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.Identity.Bff.Federation;

/// <summary>
/// Read-only view of the upstream IdPs the BFF will broker through Keycloak. Backed by
/// <see cref="IdentityFederationOptions"/>; surfaced to the SPA via <c>GET /identity/providers</c>.
/// </summary>
public interface IIdentityProviderCatalog
{
    /// <summary>Enabled providers in configuration order. Empty when federation is not configured.</summary>
    IReadOnlyList<IdentityProviderDescriptor> List();

    /// <summary>
    /// Returns <c>true</c> only when <paramref name="alias"/> matches an enabled entry. The
    /// <c>/identity/login</c> handler must call this before forwarding a caller-supplied alias
    /// to Keycloak — otherwise an attacker can probe arbitrary Keycloak broker aliases.
    /// </summary>
    bool IsKnown(string alias);
}

/// <summary>Public projection of an <see cref="IdentityProviderEntry"/>.</summary>
public sealed record IdentityProviderDescriptor(string Alias, string DisplayName, string? IconUri);

/// <summary>
/// Resolves the catalog from configuration. Uses <see cref="IOptionsMonitor{T}"/> so live config
/// reloads land without a host restart — useful in environments where the IdP roster changes
/// independently of deployments.
/// </summary>
public sealed class ConfiguredIdentityProviderCatalog(IOptionsMonitor<IdentityFederationOptions> options)
    : IIdentityProviderCatalog
{
    public IReadOnlyList<IdentityProviderDescriptor> List() =>
        options.CurrentValue.Providers
            .Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Alias))
            .Select(p => new IdentityProviderDescriptor(
                p.Alias,
                string.IsNullOrWhiteSpace(p.DisplayName) ? p.Alias : p.DisplayName,
                string.IsNullOrWhiteSpace(p.IconUri) ? null : p.IconUri))
            .ToList();

    public bool IsKnown(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return false;
        foreach (var entry in options.CurrentValue.Providers)
        {
            if (!entry.Enabled || string.IsNullOrWhiteSpace(entry.Alias)) continue;
            if (string.Equals(entry.Alias, alias, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
