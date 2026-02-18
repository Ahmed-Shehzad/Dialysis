using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Authorization;

/// <summary>
/// Handles <see cref="ScopeOrBypassRequirement"/>.
/// Succeeds when: (1) DevelopmentBypass is enabled in Development, or
/// (2) user is authenticated and has one of the required scopes in "scope" or "scp" claim.
/// </summary>
public sealed class ScopeOrBypassHandler : AuthorizationHandler<ScopeOrBypassRequirement>
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;

    public ScopeOrBypassHandler(IHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeOrBypassRequirement requirement)
    {
        if (_env.IsDevelopment() && _config.GetValue("Authentication:JwtBearer:DevelopmentBypass", false))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
            return Task.CompletedTask;

        string[] scopes = GetScopes(context.User);
        if (requirement.AllowedScopes.Any(allowed => scopes.Contains(allowed, StringComparer.OrdinalIgnoreCase))) context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static string[] GetScopes(ClaimsPrincipal user)
    {
        List<string> scopes = [];

        foreach (Claim c in user.FindAll("scope"))
            scopes.AddRange(c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        foreach (Claim c in user.FindAll("scp"))
            scopes.AddRange(c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return [.. scopes];
    }
}
