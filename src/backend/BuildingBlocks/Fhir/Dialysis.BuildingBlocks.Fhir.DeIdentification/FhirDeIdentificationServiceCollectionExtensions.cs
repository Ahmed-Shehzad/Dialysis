using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

public static class FhirDeIdentificationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the HIPAA de-identifier (<see cref="SafeHarborDeIdentifier"/>) for all three
        /// <see cref="DeIdentificationProfile"/>s. Pass <paramref name="configureCustom"/> to supply the
        /// field rules the <see cref="DeIdentificationProfile.Custom"/> profile uses; omit it and Custom
        /// falls back to the strict Safe Harbor-equivalent defaults.
        /// </summary>
        public IServiceCollection AddFhirDeIdentification(Action<CustomDeIdentificationRules>? configureCustom = null)
        {
            if (configureCustom is not null)
            {
                var rules = new CustomDeIdentificationRules();
                configureCustom(rules);
                services.TryAddSingleton(rules);
            }

            services.TryAddSingleton<IFhirDeIdentifier, SafeHarborDeIdentifier>();
            return services;
        }
    }
}
