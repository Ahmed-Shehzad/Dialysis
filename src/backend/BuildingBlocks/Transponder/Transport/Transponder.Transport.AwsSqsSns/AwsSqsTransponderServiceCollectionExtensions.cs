using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

public static class AwsSqsTransponderServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces the default in-process <see cref="ITransponderBus"/> with Amazon SQS (single standard queue).
        /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/>.
        /// </summary>
        public IServiceCollection AddTransponderAwsSqs(
            Action<TransponderAwsSqsOptions> configureOptions,
            Action<AwsSqsSubscriptionBuilder>? configureSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            services.AddLogging();
            services.Configure(configureOptions);

            var registry = new AwsSqsSubscriptionRegistry();
            configureSubscriptions?.Invoke(new AwsSqsSubscriptionBuilder(services, registry));
            new AwsSqsSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
            services.AddSingleton(registry);

            services.RemoveDescriptorsFor(typeof(ITransponderBus));
            services.AddSingleton<ITransponderTransport, AwsSqsTransponderTransport>();
            services.AddSingleton<ITransponderBus, AwsSqsTransponderBus>();
            services.AddHostedService<AwsSqsTransponderConsumerHostedService>();
            return services;
        }
    }
}
