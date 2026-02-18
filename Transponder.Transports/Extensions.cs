using Microsoft.Extensions.DependencyInjection;

namespace Transponder.Transports;

/// <summary>
/// Extension methods to register Transponder transport services.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers Transponder transports using the provided configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The transport configuration callback.</param>
    public static IServiceCollection AddTransponderTransports(
        this IServiceCollection services,
        Action<TransponderTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TransponderTransportBuilder(services);
        configure(builder);
        builder.Build();
        return services;
    }
}
