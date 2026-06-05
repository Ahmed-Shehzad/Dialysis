using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Module.Bff.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dialysis.Module.Bff.Events;

/// <summary>
/// Adds event-driven push to a per-context BFF: a SignalR notifications hub plus a consume-only
/// Transponder/RabbitMQ subscription. Call <see cref="AddModuleBffEvents"/> after
/// <c>AddModuleBff()</c> and <see cref="MapModuleBffEvents"/> after <c>MapModuleBff()</c>.
/// </summary>
public static class ModuleBffEventsExtensions
{
    /// <summary>
    /// Registers the SignalR hub + <see cref="IBffNotifier"/> and a consume-only Transponder bus.
    /// Pass <paramref name="configureConsumers"/> to register the integration-event consumers this
    /// context cares about — each consumer maps its event to a <see cref="BffNotification"/> and
    /// calls <see cref="IBffNotifier"/>. Registering a consumer is enough for RabbitMQ to bind the
    /// BFF's queue to the producer's exchange; no explicit subscription list is needed.
    /// </summary>
    public static WebApplicationBuilder AddModuleBffEvents(
        this WebApplicationBuilder builder,
        Action<TransponderBuilder>? configureConsumers = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var config = builder.Configuration;
        var services = builder.Services;

        var module = config.GetSection(ModuleBffOptions.SectionName).Get<ModuleBffOptions>() ?? new ModuleBffOptions();
        var events = config.GetSection(BffEventsOptions.SectionName).Get<BffEventsOptions>() ?? new BffEventsOptions();
        services.Configure<BffEventsOptions>(config.GetSection(BffEventsOptions.SectionName));

        var slug = string.IsNullOrWhiteSpace(module.Slug) ? "bff" : module.Slug.Trim('/');

        // SignalR + optional Valkey/Redis backplane for cross-replica fan-out (mirrors PDMS vitals).
        var signalr = services.AddSignalR();
        var backplane = events.SignalR.BackplaneConnectionString;
        if (!string.IsNullOrWhiteSpace(backplane))
        {
            signalr.AddStackExchangeRedis(backplane, o =>
                o.Configuration.ChannelPrefix = RedisChannel.Literal(slug + "-bff-signalr"));
        }

        services.TryAddSingleton<IBffNotifier, SignalRBffNotifier>();

        // Consume-only: register the consumers; their consume routes drive the RabbitMQ queue
        // bindings. No DbContext/outbox — a BFF never originates a state change.
        services.AddTransponder(t => configureConsumers?.Invoke(t));

        var rabbitUri = events.RabbitMq.ConnectionUri;
        if (!string.IsNullOrWhiteSpace(rabbitUri))
        {
            var queue = string.IsNullOrWhiteSpace(events.RabbitMq.QueueName)
                ? "bff-" + slug
                : events.RabbitMq.QueueName;
            services.AddTransponderRabbitMq(o =>
            {
                o.ConnectionUri = rabbitUri;
                o.QueueName = queue;
            });
        }

        return builder;
    }

    /// <summary>
    /// Maps the notifications hub at <c>{BasePath}/events</c> (or <c>Bff:Events:HubPath</c> when set).
    /// Requires <c>AddModuleBff()</c> to have configured <see cref="ModuleBffOptions"/>.
    /// </summary>
    public static WebApplication MapModuleBffEvents(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var module = app.Services.GetRequiredService<IOptions<ModuleBffOptions>>().Value;
        var events = app.Services.GetService<IOptions<BffEventsOptions>>()?.Value ?? new BffEventsOptions();

        var hubPath = string.IsNullOrWhiteSpace(events.HubPath)
            ? module.ResolveBasePath() + "/events"
            : events.HubPath;

        app.MapHub<NotificationsHub>(hubPath);
        return app;
    }
}
