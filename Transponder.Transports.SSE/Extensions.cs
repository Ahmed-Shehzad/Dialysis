using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Transports.Abstractions;
using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Extension methods to register SSE transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseSse(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, ISseHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<SseTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (sp, settings) => new SseTransportHost(
                settings,
                sp.GetService<SseClientRegistry>() ?? new SseClientRegistry()));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseSse(
        this TransponderTransportRegistrationOptions options,
        ISseHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseSse(_ => settings);
    }

    public static TransponderTransportRegistrationOptions UseSse(
        this TransponderTransportRegistrationOptions options,
        Uri localAddress,
        IEnumerable<Uri>? remoteAddresses = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(localAddress);

        options.AddTransportFactory<SseTransportFactory>();
        options.AddTransportHost(
            _ => new SseHostSettings(localAddress),
            (sp, settings) => new SseTransportHost(
                settings,
                sp.GetService<SseClientRegistry>() ?? new SseClientRegistry()));

        if (remoteAddresses is null) return options;

        foreach (Uri remoteAddress in remoteAddresses)
        {
            if (remoteAddress == localAddress) continue;

            options.AddTransportHost(
                _ => new SseHostSettings(remoteAddress),
                (sp, settings) => new SseTransportHost(
                    settings,
                    sp.GetService<SseClientRegistry>() ?? new SseClientRegistry()));
        }

        return options;
    }

    public static IServiceCollection UseSse(
        this IServiceCollection services,
        Func<IServiceProvider, ISseHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddSseTransport(settingsFactory));
    }

    public static IServiceCollection UseSse(
        this IServiceCollection services,
        ISseHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseSse(_ => settings);
    }

    public static IServiceCollection UseSse(
        this IServiceCollection services,
        Uri localAddress,
        IEnumerable<Uri>? remoteAddresses = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(localAddress);

        return AddTransportRegistration(services, builder =>
        {
            _ = builder.AddTransportFactory<SseTransportFactory>();
            _ = builder.AddTransportHost<ISseHostSettings, SseTransportHost>(
                _ => new SseHostSettings(localAddress),
                (sp, settings) => new SseTransportHost(
                    settings,
                    sp.GetService<SseClientRegistry>() ?? new SseClientRegistry()));

            if (remoteAddresses is null) return;

            foreach (Uri remoteAddress in remoteAddresses)
            {
                if (remoteAddress == localAddress) continue;

                _ = builder.AddTransportHost<ISseHostSettings, SseTransportHost>(
                    _ => new SseHostSettings(remoteAddress),
                    (sp, settings) => new SseTransportHost(
                        settings,
                        sp.GetService<SseClientRegistry>() ?? new SseClientRegistry()));
            }
        });
    }

    public static TransponderTransportBuilder AddSseTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, ISseHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<SseTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (sp, settings) => new SseTransportHost(
                settings,
                sp.GetService<SseClientRegistry>() ?? new SseClientRegistry()));

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
            services.TryAddSingleton<SseClientRegistry>();
            configure(builder);
            return services;
        }

        _ = services.AddTransponderTransports(builder =>
        {
            services.TryAddSingleton<SseClientRegistry>();
            configure(builder);
        });
        return services;
    }
}
