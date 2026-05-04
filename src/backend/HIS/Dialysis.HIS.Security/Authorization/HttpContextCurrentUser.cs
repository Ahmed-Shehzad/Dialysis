using System.Security.Claims;
using Dialysis.HIS.Contracts.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Security.Authorization;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from <see cref="HttpContext.User"/> when JWT is configured; otherwise matches <see cref="DevelopmentCurrentUser"/> behavior.
/// </summary>
public sealed class HttpContextCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IOptions<HisAuthenticationOptions> options) : ICurrentUser
{
    private readonly HisAuthenticationOptions _options = options.Value;

    public string? UserId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_options.Authority))
                return "dev-user";
            var user = httpContextAccessor.HttpContext?.User;
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
                return HisPermissions.All;

            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Array.Empty<string>();

            var valid = new HashSet<string>(HisPermissions.All, StringComparer.Ordinal);
            var granted = new HashSet<string>(StringComparer.Ordinal);
            var claimType = _options.PermissionClaimType;
            foreach (var c in user.FindAll(claimType))
            {
                if (valid.Contains(c.Value))
                    granted.Add(c.Value);
            }

            var roleClaimType = _options.RoleClaimType;
            foreach (var roleClaim in user.FindAll(roleClaimType))
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

            return granted.ToList();
        }
    }
}
