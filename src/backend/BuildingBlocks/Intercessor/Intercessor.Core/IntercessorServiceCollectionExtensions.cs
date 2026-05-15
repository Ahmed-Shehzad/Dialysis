using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Intercessor;

public static class IntercessorServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="IIntercessor"/> and configures handlers, validators, and behaviors.
        /// </summary>
        public IServiceCollection AddIntercessor(Action<IntercessorBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.AddScoped<IIntercessor, Intercessor>();
            configure(new IntercessorBuilder(services));
            return services;
        }
    }
}
