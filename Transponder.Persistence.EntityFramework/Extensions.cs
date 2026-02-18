using Microsoft.Extensions.DependencyInjection;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Extension methods to register Entity Framework saga persistence.
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddEntityFrameworkSagaRepository<TState>(this IServiceCollection services)
        where TState : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddScoped<ISagaRepository<TState>, EntityFrameworkSagaRepository<TState>>();
        return services;
    }
}
