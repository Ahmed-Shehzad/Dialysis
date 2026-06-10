using Microsoft.Extensions.Hosting;

namespace Dialysis.Module.Bff.Configuration;

/// <summary>
/// Fail-fast gate for the OIDC client secret — the BFF-tier sibling of the gateway's
/// "non-empty AllowedOrigins outside Development" check. The repo ships
/// <c>*-bff-dev-secret-change-me</c> placeholders in base <c>appsettings.json</c> for the dev
/// realm; this guard makes sure a host that boots outside Development with one of those (or with
/// no secret at all) throws at startup instead of silently authenticating against Keycloak with a
/// publicly-known credential.
/// </summary>
public static class KeycloakSecretGuard
{
    /// <summary>Marker substring every shipped dev placeholder secret carries.</summary>
    private const string DevPlaceholderMarker = "change-me";

    /// <summary>
    /// Throws when the host is not Development and <paramref name="clientSecret"/> is missing or
    /// still a placeholder. No-op in Development so the F5 loop keeps working against the imported
    /// dev realm. Takes the raw secret (not an options type) because the identity BFF carries its
    /// own <c>KeycloakBffOptions</c> twin.
    /// </summary>
    public static void EnsureProductionClientSecret(
        IHostEnvironment environment,
        string? clientSecret,
        string sectionName = KeycloakBffOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.IsDevelopment())
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(clientSecret)
            || clientSecret.Contains(DevPlaceholderMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{sectionName}:ClientSecret is missing or still the dev placeholder " +
                $"('{DevPlaceholderMarker}') while ASPNETCORE_ENVIRONMENT is '{environment.EnvironmentName}'. " +
                "Set a real client secret (environment variable or secret store) before running outside Development.");
        }
    }
}
