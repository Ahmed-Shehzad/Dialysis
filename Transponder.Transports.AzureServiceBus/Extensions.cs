using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Extension methods to register Azure Service Bus transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseAzureServiceBus(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, IAzureServiceBusHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<AzureServiceBusTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (_, settings) => new AzureServiceBusTransportHost(settings));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseAzureServiceBus(
        this TransponderTransportRegistrationOptions options,
        IAzureServiceBusHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseAzureServiceBus(_ => settings);
    }

    public static IServiceCollection UseAzureServiceBus(
        this IServiceCollection services,
        Func<IServiceProvider, IAzureServiceBusHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddAzureServiceBusTransport(settingsFactory));
    }

    public static IServiceCollection UseAzureServiceBus(
        this IServiceCollection services,
        IAzureServiceBusHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseAzureServiceBus(_ => settings);
    }

    public static TransponderTransportBuilder AddAzureServiceBusTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, IAzureServiceBusHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<AzureServiceBusTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (_, settings) => new AzureServiceBusTransportHost(settings));

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
