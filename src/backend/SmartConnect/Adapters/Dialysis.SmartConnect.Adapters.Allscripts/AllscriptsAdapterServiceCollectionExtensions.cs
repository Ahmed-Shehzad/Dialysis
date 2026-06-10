using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Adapters.Allscripts;

public static class AllscriptsAdapterServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Veradigm / Allscripts FHIR R4 adapter using OAuth2 password grant.
        /// Configuration shape:
        /// <c>{ "Allscripts": { "BaseUrl": "...", "TokenEndpoint": "...", "AppName": "...", "ClientId": "...", "Username": "...", "Password": "...", "Scope": "system/Patient.read" } }</c>.
        /// </summary>
        public IServiceCollection AddAllscriptsFhirAdapter(IConfiguration allscriptsSection)
        {
            services.AddOptions<AllscriptsAdapterOptions>().Bind(allscriptsSection);
            services.AddVendorAdapterTokenAcquirer();
            services.AddResilientVendorHttpClient("Allscripts");
            services.AddSingleton<AllscriptsAuthProvider>();
            services.AddSingleton<IExternalEhrAuthProvider>(sp => sp.GetRequiredService<AllscriptsAuthProvider>());
            services.AddSingleton<AllscriptsFhirAdapter>();
            services.AddSingleton<IExternalEhrAdapter>(sp => sp.GetRequiredService<AllscriptsFhirAdapter>());
            return services;
        }
    }
}
