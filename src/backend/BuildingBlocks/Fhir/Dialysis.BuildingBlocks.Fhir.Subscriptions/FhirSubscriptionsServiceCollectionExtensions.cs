using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public static class FhirSubscriptionsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory subscription registry + REST-hook dispatcher. Production hosts can
    /// replace <see cref="ISubscriptionRegistry"/> with the EF-Core-backed implementation from
    /// <c>Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore</c>.
    /// </summary>
    public static IServiceCollection AddFhirSubscriptions(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemorySubscriptionRegistry>();
        services.TryAddSingleton<ISubscriptionRegistry>(sp => sp.GetRequiredService<InMemorySubscriptionRegistry>());
        services.TryAddSingleton<ISubscriptionMatcher>(sp => sp.GetRequiredService<InMemorySubscriptionRegistry>());
        services.TryAddSingleton<ISubscriptionNotificationDispatcher, RestHookNotificationDispatcher>();
        services.AddHttpClient(nameof(RestHookNotificationDispatcher));
        return services;
    }
}
