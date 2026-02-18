using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

/// <summary>
/// Extension methods to register RabbitMQ transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseRabbitMq(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, IRabbitMqHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<RabbitMqTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (_, settings) => new RabbitMqTransportHost(settings));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseRabbitMq(
        this TransponderTransportRegistrationOptions options,
        IRabbitMqHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseRabbitMq(_ => settings);
    }

    public static IServiceCollection UseRabbitMq(
        this IServiceCollection services,
        Func<IServiceProvider, IRabbitMqHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddRabbitMqTransport(settingsFactory));
    }

    public static IServiceCollection UseRabbitMq(
        this IServiceCollection services,
        IRabbitMqHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseRabbitMq(_ => settings);
    }

    public static TransponderTransportBuilder AddRabbitMqTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, IRabbitMqHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<RabbitMqTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (_, settings) => new RabbitMqTransportHost(settings));

        return builder;
    }

    private static IServiceCollection AddTransportRegistration(
        IServiceCollection services,
        Action<TransponderTransportBuilder> configure)
    {
        bool hasProvider = services.Any(service => service.ServiceType == typeof(ITransportHostProvider));
        bool hasRegistry = services.Any(service => service.ServiceType == typeof(ITransportRegistry));

        if (hasProvider && hasRegistry)
        {
            var builder = new TransponderTransportBuilder(services);
            configure(builder);
            return services;
        }

        _ = services.AddTransponderTransports(configure);
        return services;
    }
}
