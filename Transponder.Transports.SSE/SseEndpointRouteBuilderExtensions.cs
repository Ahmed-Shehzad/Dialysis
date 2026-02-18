using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Endpoint registration helpers for SSE.
/// </summary>
public static class SseEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapTransponderSse(
        this IEndpointRouteBuilder endpoints,
        Action<SseEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = new SseEndpointOptions();
        configure?.Invoke(options);

        ISseHostSettings settings = endpoints.ServiceProvider.GetRequiredService<ISseHostSettings>();
        string path = options.Path ?? settings.Topology.StreamPath;

        return endpoints.MapGet(path, async context =>
        {
            ISseHostSettings resolvedSettings = context.RequestServices.GetRequiredService<ISseHostSettings>();
            SseClientRegistry registry = ResolveRegistry(context.RequestServices);
            ISseCatchUpProvider? catchUpProvider = context.RequestServices.GetService<ISseCatchUpProvider>();

            await SseEndpoint.HandleAsync(
                    context,
                    resolvedSettings,
                    registry,
                    catchUpProvider,
                    options)
                .ConfigureAwait(false);
        });
    }

    private static SseClientRegistry ResolveRegistry(IServiceProvider services)
    {
        SseClientRegistry? registry = services.GetService<SseClientRegistry>();
        if (registry is not null) return registry;

        IEnumerable<ITransportHost> hosts = services.GetServices<ITransportHost>();
        SseTransportHost? sseHost = hosts.OfType<SseTransportHost>().FirstOrDefault();
        if (sseHost is not null) return sseHost.ClientRegistry;

        throw new InvalidOperationException(
            "SseClientRegistry is not registered. Register SSE transports or add SseClientRegistry.");
    }
}
