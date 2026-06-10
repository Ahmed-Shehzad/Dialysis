using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Adapters.Cerner;

public static class CernerAdapterServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Cerner FHIR R4 adapter with OAuth2 client_credentials + Basic auth. Configuration:
        /// <c>{ "Cerner": { "BaseUrl": "...", "TokenEndpoint": "...", "ClientId": "...", "ClientSecret": "...", "Scope": "system/Patient.read" } }</c>.
        /// </summary>
        public IServiceCollection AddCernerFhirAdapter(IConfiguration cernerSection)
        {
            services.AddOptions<CernerAdapterOptions>().Bind(cernerSection);
            services.AddVendorAdapterTokenAcquirer();
            services.AddResilientVendorHttpClient("Cerner");
            services.AddSingleton<CernerAuthProvider>();
            services.AddSingleton<IExternalEhrAuthProvider>(sp => sp.GetRequiredService<CernerAuthProvider>());
            services.AddSingleton<CernerFhirAdapter>();
            services.AddSingleton<IExternalEhrAdapter>(sp => sp.GetRequiredService<CernerFhirAdapter>());
            return services;
        }
    }
}
