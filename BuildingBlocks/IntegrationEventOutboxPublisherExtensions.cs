using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks;

public static class IntegrationEventOutboxPublisherExtensions
{
    public static IServiceCollection AddIntegrationEventOutboxPublisher<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        _ = services.AddHostedService<IntegrationEventOutboxPublisher<TContext>>();
        return services;
    }
}
