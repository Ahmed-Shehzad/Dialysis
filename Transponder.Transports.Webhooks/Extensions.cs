using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Webhooks.Abstractions;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Extension methods to register webhook transports.
/// </summary>
public static class Extensions
{
    public static TransponderTransportRegistrationOptions UseWebhooks(
        this TransponderTransportRegistrationOptions options,
        Func<IServiceProvider, IWebhookHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        options.AddTransportFactory<WebhookTransportFactory>();
        options.AddTransportHost(
            settingsFactory,
            (_, settings) => new WebhookTransportHost(settings));

        return options;
    }

    public static TransponderTransportRegistrationOptions UseWebhooks(
        this TransponderTransportRegistrationOptions options,
        IWebhookHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        return options.UseWebhooks(_ => settings);
    }

    public static TransponderTransportRegistrationOptions UseWebhooks(
        this TransponderTransportRegistrationOptions options,
        Uri address,
        IReadOnlyList<WebhookSubscription> subscriptions,
        Action<WebhookSignatureOptions>? configureSignature = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(subscriptions);

        var signature = new WebhookSignatureOptions();
        configureSignature?.Invoke(signature);

        options.AddTransportFactory<WebhookTransportFactory>();
        options.AddTransportHost(_ => new WebhookTransportHost(new WebhookHostSettings(
            address,
            subscriptions,
            signature)));

        return options;
    }

    public static IServiceCollection UseWebhooks(
        this IServiceCollection services,
        Func<IServiceProvider, IWebhookHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        return AddTransportRegistration(services, builder => builder.AddWebhooksTransport(settingsFactory));
    }

    public static IServiceCollection UseWebhooks(
        this IServiceCollection services,
        IWebhookHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        return services.UseWebhooks(_ => settings);
    }

    public static IServiceCollection UseWebhooks(
        this IServiceCollection services,
        Uri address,
        IReadOnlyList<WebhookSubscription> subscriptions,
        Action<WebhookSignatureOptions>? configureSignature = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(subscriptions);

        var signature = new WebhookSignatureOptions();
        configureSignature?.Invoke(signature);

        return AddTransportRegistration(services, builder =>
        {
            _ = builder.AddTransportFactory<WebhookTransportFactory>();
            _ = builder.AddTransportHost<IWebhookHostSettings, WebhookTransportHost>(
                _ => new WebhookHostSettings(address, subscriptions, signature),
                (_, settings) => new WebhookTransportHost(settings));
        });
    }

    public static TransponderTransportBuilder AddWebhooksTransport(
        this TransponderTransportBuilder builder,
        Func<IServiceProvider, IWebhookHostSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settingsFactory);

        _ = builder.AddTransportFactory<WebhookTransportFactory>();
        _ = builder.AddTransportHost(
            settingsFactory,
            (_, settings) => new WebhookTransportHost(settings));

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
