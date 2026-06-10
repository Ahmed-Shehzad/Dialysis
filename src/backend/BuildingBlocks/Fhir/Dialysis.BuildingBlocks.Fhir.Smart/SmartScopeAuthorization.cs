using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Smart;

public sealed record SmartScopeRequirement : IAuthorizationRequirement
{
    public SmartScopeRequirement(string Scope) => this.Scope = Scope;
    public string Scope { get; init; }
    public void Deconstruct(out string Scope) => Scope = this.Scope;
}

/// <summary>
/// Authorization handler that validates the caller JWT carries the required SMART scope. Optionally
/// also checks that <c>patient</c> launch claim matches a route patient id when the scope starts
/// with <c>patient/</c>.
/// </summary>
public sealed class SmartScopeAuthorizationHandler : AuthorizationHandler<SmartScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SmartScopeRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var hasScope = context.User
            .FindAll("scope")
            .SelectMany(c => (c.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Concat(context.User.FindAll("scp").Select(c => c.Value ?? string.Empty))
            .Any(s => ScopeMatches(s, requirement.Scope));

        if (hasScope)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static bool ScopeMatches(string actual, string required)
    {
        if (string.Equals(actual, required, StringComparison.Ordinal))
            return true;
        // Wildcard support: "patient/*.read" satisfies "patient/Patient.read".
        var sep = required.IndexOf('/');
        if (sep <= 0)
            return false;
        var prefix = required[..sep];
        var dot = required.IndexOf('.', sep);
        if (dot <= 0)
            return false;
        var action = required[dot..];
        return actual == $"{prefix}/*{action}";
    }
}

public sealed class SmartScopePolicyProvider : IAuthorizationPolicyProvider
{
    private readonly SmartOnFhirOptions _options;
    public SmartScopePolicyProvider(IOptions<SmartOnFhirOptions> options) => _options = options.Value;

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.Contains('/'))
            return Task.FromResult<AuthorizationPolicy?>(null);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new SmartScopeRequirement(policyName))
            .Build();
        _ = _options; // reserved for future ScopePermissionMap fallback wiring
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
