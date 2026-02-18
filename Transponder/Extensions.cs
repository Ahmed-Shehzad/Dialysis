using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder;

/// <summary>
/// Extension methods to register Transponder services.
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddTransponder(
        this IServiceCollection services,
        Uri address,
        Action<TransponderBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(address);

        var options = new TransponderBusOptions(address);
        configure?.Invoke(options);

        return services.AddTransponder(options);
    }

    public static IServiceCollection AddTransponder(
        this IServiceCollection services,
        TransponderBusOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        if (options.TransportBuilder.HasRegistrations)
            options.TransportBuilder.Apply(services);

        services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

        if (options.OutboxOptions is not null)
        {
            _ = services.AddSingleton(options.OutboxOptions);
            _ = services.AddSingleton<OutboxDispatcher>(sp => new OutboxDispatcher(
                sp.GetRequiredService<IStorageSessionFactory>(),
                sp.GetRequiredService<ITransportHostProvider>(),
                options.OutboxOptions,
                sp.GetRequiredService<ILogger<OutboxDispatcher>>()));
        }

        _ = services.AddSingleton(sp =>
        {
            Func<Type, Uri?> resolver = options.RequestAddressResolver
                                        ?? TransponderRequestAddressResolver.Create(
                                            options.Address,
                                            options.RequestPathPrefix,
                                            options.RequestPathFormatter);

            Func<IServiceProvider, TransponderBus, IMessageScheduler> schedulerFactory = options.SchedulerFactory
                                                                                         ?? ((_, bus) => new InMemoryMessageScheduler(bus));

            return new TransponderBus(
                options.Address,
                sp.GetRequiredService<ITransportHostProvider>(),
                sp.GetServices<ITransportHost>(),
                sp.GetRequiredService<IMessageSerializer>(),
                new TransponderBusRuntimeOptions
                {
                    RequestAddressResolver = resolver,
                    DefaultRequestTimeout = options.DefaultRequestTimeout,
                    SchedulerFactory = bus => schedulerFactory(sp, bus),
                    ReceiveEndpoints = sp.GetServices<IReceiveEndpoint>(),
                    OutboxDispatcher = sp.GetService<OutboxDispatcher>(),
                    MessageScopeProviders = sp.GetServices<ITransponderMessageScopeProvider>(),
                    LoggerFactory = sp.GetService<ILoggerFactory>()
                });
        });

        _ = services.AddSingleton<IBus>(sp => sp.GetRequiredService<TransponderBus>());
        _ = services.AddSingleton<IBusControl>(sp => sp.GetRequiredService<TransponderBus>());
        _ = services.AddSingleton<IClientFactory>(sp => sp.GetRequiredService<TransponderBus>());
        _ = services.AddSingleton<IPublishEndpoint>(sp => sp.GetRequiredService<TransponderBus>());
        _ = services.AddSingleton<ISendEndpointProvider>(sp => sp.GetRequiredService<TransponderBus>());
        _ = services.AddSingleton<IMessageScheduler>(sp => sp.GetRequiredService<TransponderBus>());

        return services;
    }

    public static TransponderBusOptions UsePersistedMessageScheduler(
        this TransponderBusOptions options,
        Action<PersistedMessageSchedulerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var schedulerOptions = new PersistedMessageSchedulerOptions();
        configure?.Invoke(schedulerOptions);

        options.SchedulerFactory = (sp, bus) => new PersistedMessageScheduler(
            bus,
            sp.GetRequiredService<IScheduledMessageStore>(),
            sp.GetRequiredService<IMessageSerializer>(),
            sp.GetRequiredService<ITransportHostProvider>(),
            schedulerOptions,
            sp.GetService<ILogger<PersistedMessageScheduler>>());

        return options;
    }

    public static TransponderBusOptions UseOutbox(
        this TransponderBusOptions options,
        Action<OutboxDispatchOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var outboxOptions = new OutboxDispatchOptions();
        configure?.Invoke(outboxOptions);
        options.OutboxOptions = outboxOptions;
        return options;
    }
}
