using System.Security.Claims;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Smart;

public sealed class SmartScopeAuthorizationTests
{
    [Theory]
    [InlineData("system/*.read", "system/*.read", true)]
    [InlineData("patient/*.read", "patient/Patient.read", true)]
    [InlineData("system/Patient.read", "system/Patient.read", true)]
    [InlineData("system/*.write", "system/Patient.read", false)]
    [InlineData("user/Observation.read", "patient/Observation.read", false)]
    public async Task Handler_Succeeds_Only_When_Token_Carries_Required_Or_Wildcard_Scope_Async(
        string tokenScopes,
        string required,
        bool expectSucceed)
    {
        var handler = new SmartScopeAuthorizationHandler();
        var principal = MakePrincipal(tokenScopes);
        var requirement = new SmartScopeRequirement(required);
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.ShouldBe(expectSucceed);
    }

    [Fact]
    public async Task Handler_Reads_Space_Separated_Scopes_From_Single_Claim_Async()
    {
        var handler = new SmartScopeAuthorizationHandler();
        var principal = MakePrincipal("openid profile patient/*.read offline_access");
        var requirement = new SmartScopeRequirement("patient/Patient.read");
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Handler_Fails_For_Unauthenticated_Principal_Async()
    {
        var handler = new SmartScopeAuthorizationHandler();
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // no auth type → not authenticated
        var requirement = new SmartScopeRequirement("system/*.read");
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Policyprovider_Synthesizes_Policy_For_Scope_Shaped_Name_Async()
    {
        var options = Options.Create(new SmartOnFhirOptions
        {
            Issuer = "https://example.test/",
            AuthorizationEndpoint = "https://example.test/oauth/authorize",
            TokenEndpoint = "https://example.test/oauth/token",
        });
        var provider = new SmartScopePolicyProvider(options);

        var policy = await provider.GetPolicyAsync("system/Patient.read");

        policy.ShouldNotBeNull();
        policy!.Requirements.OfType<SmartScopeRequirement>().ShouldHaveSingleItem().Scope.ShouldBe("system/Patient.read");
    }

    [Fact]
    public async Task Policyprovider_Returns_Null_For_Non_Scope_Shaped_Policy_Names_Async()
    {
        var options = Options.Create(new SmartOnFhirOptions
        {
            Issuer = "https://example.test/",
            AuthorizationEndpoint = "https://example.test/oauth/authorize",
            TokenEndpoint = "https://example.test/oauth/token",
        });
        var provider = new SmartScopePolicyProvider(options);

        var policy = await provider.GetPolicyAsync("his.patients.read"); // dotted permission, no slash

        policy.ShouldBeNull();
    }

    private static ClaimsPrincipal MakePrincipal(string scopeClaim)
    {
        var identity = new ClaimsIdentity(authenticationType: "test");
        identity.AddClaim(new Claim("scope", scopeClaim));
        return new ClaimsPrincipal(identity);
    }
}
