using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Smart;

public static class FhirSmartServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFhirSmartOnFhir(IConfiguration smartSection)
        {
            services.Configure<SmartOnFhirOptions>(smartSection);
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<IFhirLaunchContextAccessor, HttpContextFhirLaunchContextAccessor>();
            services.AddSingleton<IAuthorizationHandler, SmartScopeAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, SmartScopePolicyProvider>();
            return services;
        }
    }
}
