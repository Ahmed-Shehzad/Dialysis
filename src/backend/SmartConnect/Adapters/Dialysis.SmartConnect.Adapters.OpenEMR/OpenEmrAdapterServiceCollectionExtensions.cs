using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Adapters.OpenEMR;

public static class OpenEmrAdapterServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the OpenEMR FHIR R4 adapter. Configuration shape:
        /// <c>{ "OpenEMR": { "BaseUrl": "...", "TokenEndpoint": "...", "ClientId": "...", "ClientSecret": "...", "Scope": "system/Patient.read" } }</c>.
        /// </summary>
        public IServiceCollection AddOpenEmrFhirAdapter(IConfiguration openEmrSection)
        {
            services.AddOptions<OpenEmrAdapterOptions>().Bind(openEmrSection);
            services.AddVendorAdapterTokenAcquirer();
            services.AddResilientVendorHttpClient("OpenEMR");
            services.AddSingleton<OpenEmrAuthProvider>();
            services.AddSingleton<IExternalEhrAuthProvider>(sp => sp.GetRequiredService<OpenEmrAuthProvider>());
            services.AddSingleton<OpenEmrFhirAdapter>();
            services.AddSingleton<IExternalEhrAdapter>(sp => sp.GetRequiredService<OpenEmrFhirAdapter>());
            return services;
        }
    }
}
