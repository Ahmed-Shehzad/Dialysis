using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Abstractions;

namespace Transponder.Serilog;

/// <summary>
/// Extension methods to enable Serilog-based message scopes.
/// </summary>
public static class Extensions
{
    public static IServiceCollection UseSerilog(
        this IServiceCollection services,
        Action<TransponderSerilogOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new TransponderSerilogOptions();
        configure?.Invoke(options);

        _ = services.AddSingleton(options);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ITransponderMessageScopeProvider, SerilogMessageScopeProvider>());

        return services;
    }
}
