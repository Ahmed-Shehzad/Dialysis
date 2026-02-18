using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authorization;

/// <summary>
/// Requires either DevelopmentBypass (when enabled in dev) or one of the allowed scopes.
/// </summary>
public sealed class ScopeOrBypassRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> AllowedScopes { get; }

    public ScopeOrBypassRequirement(params string[] allowedScopes)
    {
        AllowedScopes = allowedScopes;
    }
}
