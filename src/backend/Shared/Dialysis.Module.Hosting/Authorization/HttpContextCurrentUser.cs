using System.Security.Claims;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Dialysis.Module.Hosting.Authorization;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from <see cref="HttpContext.User"/> when an OIDC authority is configured;
/// otherwise grants every permission declared by the registered <see cref="IModulePermissionCatalog"/> for local dev.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly ModuleAuthenticationOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IModulePermissionCatalog _permissionCatalog;
    /// <summary>
    /// Resolves <see cref="ICurrentUser"/> from <see cref="HttpContext.User"/> when an OIDC authority is configured;
    /// otherwise grants every permission declared by the registered <see cref="IModulePermissionCatalog"/> for local dev.
    /// </summary>
    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor,
        IOptions<ModuleAuthenticationOptions> options,
        IModulePermissionCatalog permissionCatalog)
    {
        _httpContextAccessor = httpContextAccessor;
        _permissionCatalog = permissionCatalog;
        _options = options.Value;
    }

    public string? UserId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_options.Authority))
                return _options.DevelopmentUserId;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
        }
    }

    public IReadOnlyCollection<string> Permissions
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_options.Authority))
                return _permissionCatalog.All;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Array.Empty<string>();

            var valid = new HashSet<string>(_permissionCatalog.All, StringComparer.Ordinal);
            var granted = new HashSet<string>(StringComparer.Ordinal);

            foreach (var c in user.FindAll(_options.PermissionClaimType).Where(c => valid.Contains(c.Value)))
            {
                granted.Add(c.Value);
            }

            foreach (var roleClaim in user.FindAll(_options.RoleClaimType))
            {
                if (string.IsNullOrWhiteSpace(roleClaim.Value))
                    continue;
                if (!_options.RolePermissionMap.TryGetValue(roleClaim.Value.Trim(), out var mapped))
                    continue;
                foreach (var p in mapped)
                {
                    if (valid.Contains(p))
                        granted.Add(p);
                }
            }

            return [.. granted];
        }
    }
}
