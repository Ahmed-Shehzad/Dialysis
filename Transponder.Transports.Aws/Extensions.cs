using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Aws.Abstractions;

namespace Transponder.Transports.Aws;

/// <summary>
/// Extension methods to register AWS transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseAws(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, IAwsTransportHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<AwsTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (_, settings) => new AwsTransportHost(settings));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseAws(
        this TransponderTransportRegistrationOptions options,
        IAwsTransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseAws(_ => settings);
    }

    public static IServiceCollection UseAws(
        this IServiceCollection services,
        Func<IServiceProvider, IAwsTransportHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddAwsTransport(settingsFactory));
    }

    public static IServiceCollection UseAws(
        this IServiceCollection services,
        IAwsTransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseAws(_ => settings);
    }

    public static TransponderTransportBuilder AddAwsTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, IAwsTransportHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<AwsTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (_, settings) => new AwsTransportHost(settings));

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
