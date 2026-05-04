using System.Security.Claims;
using Dialysis.HIS.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Api.Authorization;

/// <summary>When JWT is configured, ensures portal route <c>patientId</c> matches token subject or <c>his_patient_id</c> (or configured claim).</summary>
public sealed class PatientPortalPatientScopeFilter(IOptions<HisAuthenticationOptions> options) : IAsyncAuthorizationFilter
{
    private readonly HisAuthenticationOptions _options = options.Value;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.Authority))
            return Task.CompletedTask;

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        if (!context.RouteData.Values.TryGetValue("patientId", out var raw) || raw is not string s || !Guid.TryParse(s, out var routePatientId))
            return Task.CompletedTask;

        var user = context.HttpContext.User;
        var pid = routePatientId.ToString("D");
        foreach (var claimType in new[] { _options.PatientPortalPatientIdClaimType, "patient_id", "pid" })
        {
            var v = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrEmpty(v) && string.Equals(v, pid, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;
        }

        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(sub) && string.Equals(sub, pid, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        context.Result = new ForbidResult();
        return Task.CompletedTask;
    }
}
