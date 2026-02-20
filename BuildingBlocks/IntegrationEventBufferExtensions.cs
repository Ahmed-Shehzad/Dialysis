using BuildingBlocks.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks;

public static class IntegrationEventBufferExtensions
{
    public static IServiceCollection AddIntegrationEventBuffer(this IServiceCollection services) =>
        services.AddScoped<IIntegrationEventBuffer, IntegrationEventBuffer>();
}
