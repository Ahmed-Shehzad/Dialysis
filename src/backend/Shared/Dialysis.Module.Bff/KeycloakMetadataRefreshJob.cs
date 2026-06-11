using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Module.Bff;

/// <summary>
/// The BFF's owned Hangfire recurring job: proactively re-fetches the Keycloak OIDC discovery
/// document + JWKS instead of letting the first post-rotation login pay the lazy-refresh cost.
/// Doubles as an ops heartbeat for this host's BFF→Keycloak edge — when Keycloak is unreachable
/// or the realm is misconfigured the failure surfaces as a red recurring job on this BFF's own
/// <c>/hangfire</c> dashboard, per host, with no PHI involved.
/// </summary>
public sealed class KeycloakMetadataRefreshJob
{
    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptions;
    private readonly ILogger<KeycloakMetadataRefreshJob> _logger;

    /// <summary>Resolved from DI by Hangfire each time the recurring job fires.</summary>
    public KeycloakMetadataRefreshJob(
        IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
        ILogger<KeycloakMetadataRefreshJob> logger)
    {
        _oidcOptions = oidcOptions;
        _logger = logger;
    }

    /// <summary>
    /// Forces a refresh of the OIDC configuration manager and fetches the current metadata.
    /// Throws (failing the Hangfire job, visibly) when Keycloak does not answer.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var options = _oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var configurationManager = options.ConfigurationManager;
        if (configurationManager is null)
        {
            _logger.LogWarning("No OIDC configuration manager on scheme {Scheme}; nothing to refresh.",
                OpenIdConnectDefaults.AuthenticationScheme);
            return;
        }

        // RequestRefresh marks the cached document stale (subject to the manager's refresh-interval
        // throttle); the Get call then performs the actual fetch against Keycloak.
        configurationManager.RequestRefresh();
        var configuration = await configurationManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Keycloak OIDC metadata refreshed: issuer {Issuer}, {SigningKeyCount} signing key(s).",
            configuration.Issuer,
            configuration.SigningKeys.Count);
    }
}
