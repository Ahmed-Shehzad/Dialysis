using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

public static class FhirCdaBridgeServiceCollectionExtensions
{
    public static IServiceCollection AddFhirCdaBridge(this IServiceCollection services)
    {
        services.TryAddSingleton<ICdaToFhirMapper, DefaultCdaToFhirMapper>();
        services.TryAddSingleton<IFhirToCdaMapper, DefaultFhirToCdaMapper>();
        return services;
    }
}
