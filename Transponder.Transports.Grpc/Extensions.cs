using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Grpc.Abstractions;

namespace Transponder.Transports.Grpc;

/// <summary>
/// Extension methods to register gRPC transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseGrpc(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, IGrpcHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<GrpcTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (_, settings) => new GrpcTransportHost(settings));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseGrpc(
        this TransponderTransportRegistrationOptions options,
        IGrpcHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseGrpc(_ => settings);
    }

    public static TransponderTransportRegistrationOptions UseGrpc(
        this TransponderTransportRegistrationOptions options,
        Uri localAddress,
        IEnumerable<Uri>? remoteAddresses = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(localAddress);

        options.AddTransportFactory<GrpcTransportFactory>();
        options.AddTransportHost(_ => new GrpcTransportHost(new GrpcHostSettings(
            localAddress,
            useTls: string.Equals(localAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))));

        if (remoteAddresses is null) return options;

        foreach (Uri remoteAddress in remoteAddresses)
        {
            if (remoteAddress == localAddress) continue;

            options.AddTransportHost(_ => new GrpcTransportHost(new GrpcHostSettings(
                remoteAddress,
                useTls: string.Equals(remoteAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))));
        }

        return options;
    }

    public static IServiceCollection UseGrpc(
        this IServiceCollection services,
        Func<IServiceProvider, IGrpcHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddGrpcTransport(settingsFactory));
    }

    public static IServiceCollection UseGrpc(
        this IServiceCollection services,
        IGrpcHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseGrpc(_ => settings);
    }

    public static IServiceCollection UseGrpc(
        this IServiceCollection services,
        Uri localAddress,
        IEnumerable<Uri>? remoteAddresses = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(localAddress);

        return AddTransportRegistration(services, builder =>
        {
            _ = builder.AddTransportFactory<GrpcTransportFactory>();
            _ = builder.AddTransportHost<IGrpcHostSettings, GrpcTransportHost>(
                _ => new GrpcHostSettings(
                    localAddress,
                    useTls: string.Equals(localAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)),
                (_, settings) => new GrpcTransportHost(settings));

            if (remoteAddresses is null) return;

            foreach (Uri remoteAddress in remoteAddresses)
            {
                if (remoteAddress == localAddress) continue;

                _ = builder.AddTransportHost<IGrpcHostSettings, GrpcTransportHost>(
                    _ => new GrpcHostSettings(
                        remoteAddress,
                        useTls: string.Equals(remoteAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)),
                    (_, settings) => new GrpcTransportHost(settings));
            }
        });
    }

    public static TransponderTransportBuilder AddGrpcTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, IGrpcHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<GrpcTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (_, settings) => new GrpcTransportHost(settings));

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
