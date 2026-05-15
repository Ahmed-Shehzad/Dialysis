using Dialysis.HIE.Xds.Bridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.HIE.Xds;

public static class HieXdsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHieXdsBridge()
        {
            var bridge = new DefaultXdsFhirBridge();
            services.TryAddSingleton<IXdsToFhirMapper>(bridge);
            services.TryAddSingleton<IFhirToXdsMapper>(bridge);
            return services;
        }
    }
}
