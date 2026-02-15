using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Abstractions;

namespace Transponder.OpenTelemetry;

/// <summary>
/// Extension methods to enable OpenTelemetry instrumentation.
/// </summary>
public static class Extensions
{
    public static IServiceCollection UseOpenTelemetry(
        this IServiceCollection services,
        Action<TransponderOpenTelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new TransponderOpenTelemetryOptions();
        configure?.Invoke(options);

        _ = services.AddSingleton(options);
        services.TryAddSingleton<TransponderOpenTelemetryInstrumentation>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ITransponderMessageScopeProvider, OpenTelemetryMessageScopeProvider>());

        return services;
    }
}
