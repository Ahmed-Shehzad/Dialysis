using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.SignalR.Abstractions;

namespace Transponder.Transports.SignalR;

/// <summary>
/// Extension methods to register SignalR transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseSignalR(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, ISignalRHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<SignalRTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (sp, settings) => new SignalRTransportHost(
                settings,
                sp.GetRequiredService<IHubContext<TransponderSignalRHub>>()));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseSignalR(
        this TransponderTransportRegistrationOptions options,
        ISignalRHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseSignalR(_ => settings);
    }

    public static TransponderTransportRegistrationOptions UseSignalR(
        this TransponderTransportRegistrationOptions options,
        Uri localAddress,
        IEnumerable<Uri>? remoteAddresses = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(localAddress);

        options.AddTransportFactory<SignalRTransportFactory>();
        options.AddTransportHost(
            _ => new SignalRHostSettings(localAddress),
            (sp, settings) => new SignalRTransportHost(
                settings,
                sp.GetRequiredService<IHubContext<TransponderSignalRHub>>()));

        if (remoteAddresses is null) return options;

        foreach (Uri remoteAddress in remoteAddresses)
        {
            if (remoteAddress == localAddress) continue;

            options.AddTransportHost(
                _ => new SignalRHostSettings(remoteAddress),
                (sp, settings) => new SignalRTransportHost(
                    settings,
                    sp.GetRequiredService<IHubContext<TransponderSignalRHub>>()));
        }

        return options;
    }

    public static IServiceCollection UseSignalR(
        this IServiceCollection services,
        Func<IServiceProvider, ISignalRHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddSignalRTransport(settingsFactory));
    }

    public static IServiceCollection UseSignalR(
        this IServiceCollection services,
        ISignalRHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseSignalR(_ => settings);
    }

    public static IServiceCollection UseSignalR(
        this IServiceCollection services,
        Uri localAddress,
        IEnumerable<Uri>? remoteAddresses = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(localAddress);

        return AddTransportRegistration(services, builder =>
        {
            _ = builder.AddTransportFactory<SignalRTransportFactory>();
            _ = builder.AddTransportHost<ISignalRHostSettings, SignalRTransportHost>(
                _ => new SignalRHostSettings(localAddress),
                (sp, settings) => new SignalRTransportHost(
                    settings,
                    sp.GetRequiredService<IHubContext<TransponderSignalRHub>>()));

            if (remoteAddresses is null) return;

            foreach (Uri remoteAddress in remoteAddresses)
            {
                if (remoteAddress == localAddress) continue;

                _ = builder.AddTransportHost<ISignalRHostSettings, SignalRTransportHost>(
                    _ => new SignalRHostSettings(remoteAddress),
                    (sp, settings) => new SignalRTransportHost(
                        settings,
                        sp.GetRequiredService<IHubContext<TransponderSignalRHub>>()));
            }
        });
    }

    public static TransponderTransportBuilder AddSignalRTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, ISignalRHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<SignalRTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (sp, settings) => new SignalRTransportHost(
                settings,
                sp.GetRequiredService<IHubContext<TransponderSignalRHub>>()));

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
