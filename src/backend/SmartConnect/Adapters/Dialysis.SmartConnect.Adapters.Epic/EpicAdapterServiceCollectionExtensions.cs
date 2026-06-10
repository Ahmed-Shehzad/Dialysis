using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Adapters.Epic;

public static class EpicAdapterServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Epic FHIR R4 adapter with backend-services JWT auth. Configuration shape:
        /// <c>{ "Epic": { "BaseUrl": "...", "TokenEndpoint": "...", "ClientId": "...", "PrivateKeyPemPath": "...", "Scope": "system/*.read" } }</c>.
        /// </summary>
        public IServiceCollection AddEpicFhirAdapter(IConfiguration epicSection)
        {
            services.AddOptions<EpicAdapterOptions>().Bind(epicSection);
            services.AddVendorAdapterTokenAcquirer();
            services.AddResilientVendorHttpClient("Epic");
            services.AddSingleton<EpicAuthProvider>();
            services.AddSingleton<IExternalEhrAuthProvider>(sp => sp.GetRequiredService<EpicAuthProvider>());
            services.AddSingleton<EpicFhirAdapter>();
            services.AddSingleton<IExternalEhrAdapter>(sp => sp.GetRequiredService<EpicFhirAdapter>());
            return services;
        }
    }
}
