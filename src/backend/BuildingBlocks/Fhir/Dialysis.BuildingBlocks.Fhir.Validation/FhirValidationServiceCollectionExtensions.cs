using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Validation;

public static class FhirValidationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="IFhirProfileValidator"/> with the supplied profile bindings.
        /// US Core / USCDI URLs are bound by the caller via <see cref="FhirProfileMap.Require{T}(string)"/>.
        /// </summary>
        public IServiceCollection AddFhirProfileValidation(
            Action<FhirProfileMap> configure)
        {
            var map = new FhirProfileMap();
            configure(map);
            services.TryAddSingleton(map);
            services.TryAddSingleton<IFhirProfileValidator, DefaultFhirProfileValidator>();
            return services;
        }
    }
}
