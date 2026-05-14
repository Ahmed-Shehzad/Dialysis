using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Smart;

public static class SmartConfigurationEndpoint
{
    /// <summary>
    /// Maps <c>GET /.well-known/smart-configuration</c> with the SMART app launch discovery
    /// document built from <see cref="SmartOnFhirOptions"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapSmartConfigurationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/.well-known/smart-configuration", HandleAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task HandleAsync(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<SmartOnFhirOptions>>().Value;
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            issuer = options.Issuer,
            authorization_endpoint = options.AuthorizationEndpoint,
            token_endpoint = options.TokenEndpoint,
            revocation_endpoint = options.RevocationEndpoint,
            introspection_endpoint = options.IntrospectionEndpoint,
            management_endpoint = options.ManagementEndpoint,
            capabilities = options.Capabilities,
            scopes_supported = options.ScopesSupported,
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "client_credentials", "refresh_token" },
            code_challenge_methods_supported = new[] { "S256" },
        }, context.RequestAborted).ConfigureAwait(false);
    }
}
