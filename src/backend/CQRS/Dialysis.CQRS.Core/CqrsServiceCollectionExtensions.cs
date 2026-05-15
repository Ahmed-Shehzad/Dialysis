using Dialysis.BuildingBlocks.Intercessor;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.CQRS;

public static class CqrsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="ICqrsGateway"/>, <see cref="IIntercessor"/>, and CQRS handlers configured on <see cref="CqrsBuilder"/>.
        /// </summary>
        public IServiceCollection AddCqrs(Action<CqrsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.AddScoped<ICqrsGateway, CqrsGateway>();
            return services.AddIntercessor(intercessorBuilder => configure(new CqrsBuilder(intercessorBuilder, services)));
        }
    }
}
