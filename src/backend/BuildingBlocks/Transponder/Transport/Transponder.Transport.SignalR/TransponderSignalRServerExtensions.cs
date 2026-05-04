using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Registers the SignalR ingress hub used by <see cref="SignalRTransponderServiceCollectionExtensions.AddTransponderSignalR"/> clients.</summary>
public static class TransponderSignalRServerExtensions
{
    /// <summary>
    /// Binds <see cref="TransponderSignalRIngressOptions"/> and hub message size limits. Call after <c>services.AddSignalR()</c> (or equivalent) so your host owns SignalR registration.
    /// </summary>
    public static IServiceCollection AddTransponderSignalRIngressServer(
        this IServiceCollection services,
        Action<TransponderSignalRIngressOptions>? configure = null)
    {
        services.AddOptions<TransponderSignalRIngressOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<HubOptions>, TransponderSignalRIngressPostConfigureHubOptions>());

        return services;
    }

    /// <summary>Maps <see cref="TransponderSignalRHub"/> at <see cref="TransponderSignalRHub.MapPath"/>.</summary>
    public static IEndpointRouteBuilder MapTransponderSignalRHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<TransponderSignalRHub>(TransponderSignalRHub.MapPath);
        return endpoints;
    }

    /// <summary>Maps <see cref="TransponderSignalRHub"/> at <see cref="TransponderSignalRHub.MapPath"/>.</summary>
    public static WebApplication MapTransponderSignalRHub(this WebApplication app)
    {
        app.MapHub<TransponderSignalRHub>(TransponderSignalRHub.MapPath);
        return app;
    }
}
