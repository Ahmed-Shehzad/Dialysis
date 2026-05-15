using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

public static class RabbitMqTransponderServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces the default in-process <see cref="ITransponderBus"/> with RabbitMQ publish/consume.
        /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/> so consumers and serializers are registered.
        /// </summary>
        public IServiceCollection AddTransponderRabbitMq(
            Action<TransponderRabbitMqOptions> configureOptions,
            Action<RabbitMqSubscriptionBuilder>? configureSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            services.AddLogging();
            services.Configure(configureOptions);

            var registry = new RabbitMqSubscriptionRegistry();
            configureSubscriptions?.Invoke(new RabbitMqSubscriptionBuilder(services, registry));
            new RabbitMqSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
            services.AddSingleton(registry);

            services.RemoveDescriptorsFor(typeof(ITransponderBus));
            services.AddSingleton<ITransponderTransport, RabbitMqTransponderTransport>();
            services.AddSingleton<ITransponderBus, RabbitMqTransponderBus>();
            services.AddHostedService<RabbitMqTransponderConsumerHostedService>();
            return services;
        }
    }
}
