using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

public static class FhirCdaBridgeServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFhirCdaBridge()
        {
            services.TryAddSingleton<ICdaToFhirMapper, DefaultCdaToFhirMapper>();
            services.TryAddSingleton<IFhirToCdaMapper, DefaultFhirToCdaMapper>();
            return services;
        }
    }
}
