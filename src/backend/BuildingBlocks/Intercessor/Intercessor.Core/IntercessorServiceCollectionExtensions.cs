using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Intercessor;

public static class IntercessorServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IIntercessor"/> and configures handlers, validators, and behaviors.
    /// </summary>
    public static IServiceCollection AddIntercessor(this IServiceCollection services, Action<IntercessorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddScoped<IIntercessor, Intercessor>();
        configure(new IntercessorBuilder(services));
        return services;
    }
}
