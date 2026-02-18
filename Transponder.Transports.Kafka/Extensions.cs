using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Kafka.Abstractions;

namespace Transponder.Transports.Kafka;

/// <summary>
/// Extension methods to register Kafka transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseKafka(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, IKafkaHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<KafkaTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (_, settings) => new KafkaTransportHost(settings));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseKafka(
        this TransponderTransportRegistrationOptions options,
        IKafkaHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseKafka(_ => settings);
    }

    public static IServiceCollection UseKafka(
        this IServiceCollection services,
        Func<IServiceProvider, IKafkaHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddKafkaTransport(settingsFactory));
    }

    public static IServiceCollection UseKafka(
        this IServiceCollection services,
        IKafkaHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseKafka(_ => settings);
    }

    public static TransponderTransportBuilder AddKafkaTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, IKafkaHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<KafkaTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (_, settings) => new KafkaTransportHost(settings));

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
