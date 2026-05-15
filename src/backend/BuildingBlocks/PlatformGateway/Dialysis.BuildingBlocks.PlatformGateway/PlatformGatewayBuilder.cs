using Dialysis.BuildingBlocks.Fhir.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.BuildingBlocks.PlatformGateway;

public sealed class PlatformGatewayBuilder
{
    private readonly IEndpointRouteBuilder _endpoints;
    private string _baseUrl = "/fhir";

    internal PlatformGatewayBuilder(IEndpointRouteBuilder endpoints) => _endpoints = endpoints;

    public PlatformGatewayBuilder WithFhir(Action<FhirEndpointOptions>? configure = null)
    {
        _endpoints.MapFhirEndpoints(o =>
        {
            o.BaseUrl = _baseUrl;
            configure?.Invoke(o);
        });
        return this;
    }

    public PlatformGatewayBuilder WithBaseUrl(string url)
    {
        _baseUrl = url;
        return this;
    }

    public PlatformGatewayBuilder WithSmart()
    {
        _endpoints.MapSmartConfigurationEndpoint();
        return this;
    }

    public PlatformGatewayBuilder WithBulkData()
    {
        _endpoints.MapFhirBulkDataEndpoints(_baseUrl);
        return this;
    }
}

public static class PlatformGatewayExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Single ergonomic facade — composes FHIR + SMART + bulk-data endpoints in one call so module
        /// hosts don't need to remember the individual <c>Map*</c> extensions.
        /// </summary>
        public IEndpointRouteBuilder MapPlatformApis(Action<PlatformGatewayBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            var builder = new PlatformGatewayBuilder(endpoints);
            configure(builder);
            return endpoints;
        }
    }
}
