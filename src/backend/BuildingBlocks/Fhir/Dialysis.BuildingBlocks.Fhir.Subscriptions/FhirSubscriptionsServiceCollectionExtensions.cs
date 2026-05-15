using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public static class FhirSubscriptionsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the in-memory subscription registry + REST-hook dispatcher. Production hosts can
        /// replace <see cref="ISubscriptionRegistry"/> with the EF-Core-backed implementation from
        /// <c>Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore</c>.
        /// </summary>
        public IServiceCollection AddFhirSubscriptions(
            Action<SubscriptionTopicCatalog>? configureTopics = null)
        {
            services.TryAddSingleton<InMemorySubscriptionRegistry>();
            services.TryAddSingleton<ISubscriptionRegistry>(sp => sp.GetRequiredService<InMemorySubscriptionRegistry>());
            services.TryAddSingleton<ISubscriptionMatcher>(sp => sp.GetRequiredService<InMemorySubscriptionRegistry>());

            // Channel adapters (REST-hook ships as the durable production path; WebSocket + SSE are
            // connection-scoped push). The composite routes by the subscription's channel type.
            services.TryAddSingleton<FhirSubscriptionConnectionManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubscriptionChannelDispatcher, RestHookNotificationDispatcher>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubscriptionChannelDispatcher, WebSocketNotificationDispatcher>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubscriptionChannelDispatcher, ServerSentEventsNotificationDispatcher>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubscriptionChannelDispatcher, EmailNotificationDispatcher>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubscriptionChannelDispatcher, SmsNotificationDispatcher>());
            services.TryAddSingleton<ISubscriptionNotificationDispatcher, CompositeSubscriptionNotificationDispatcher>();
            services.TryAddSingleton<SubscriptionBroadcaster>();
            services.AddHttpClient(nameof(RestHookNotificationDispatcher));

            // The topic catalog is a singleton mutated at composition time. Resolve any existing
            // instance, otherwise create a fresh one, register it, and apply the configurator.
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(SubscriptionTopicCatalog));
            var catalog = existing?.ImplementationInstance as SubscriptionTopicCatalog;
            if (catalog is null)
            {
                catalog = new SubscriptionTopicCatalog();
                services.AddSingleton(catalog);
            }
            configureTopics?.Invoke(catalog);

            return services;
        }
    }
}
