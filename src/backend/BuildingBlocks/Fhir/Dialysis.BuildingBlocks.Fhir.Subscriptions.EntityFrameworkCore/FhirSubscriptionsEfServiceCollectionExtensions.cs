using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;

public static class FhirSubscriptionsEfServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-memory <see cref="ISubscriptionRegistry"/> and
    /// <see cref="ISubscriptionMatcher"/> with EF Core-backed implementations on the module's
    /// <typeparamref name="TDbContext"/>. The module is responsible for applying
    /// <see cref="SubscriptionRecordConfiguration"/> and
    /// <see cref="NotificationOutboxRecordConfiguration"/> in its <c>OnModelCreating</c> override.
    /// </summary>
    public static IServiceCollection AddFhirSubscriptionsEntityFrameworkStore<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<EfSubscriptionRegistry<TDbContext>>();
        services.AddScoped<ISubscriptionRegistry>(sp => sp.GetRequiredService<EfSubscriptionRegistry<TDbContext>>());
        services.AddScoped<ISubscriptionMatcher>(sp => sp.GetRequiredService<EfSubscriptionRegistry<TDbContext>>());
        return services;
    }
}
