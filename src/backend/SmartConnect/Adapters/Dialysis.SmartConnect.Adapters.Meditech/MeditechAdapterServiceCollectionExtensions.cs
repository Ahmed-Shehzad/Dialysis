using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Adapters.Meditech;

public static class MeditechAdapterServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Meditech Expanse FHIR R4 adapter. Configuration shape:
        /// <c>{ "Meditech": { "BaseUrl": "...", "TokenEndpoint": "...", "ClientId": "...", "ClientSecret": "...", "Scope": "system/Patient.read" } }</c>.
        /// </summary>
        public IServiceCollection AddMeditechFhirAdapter(IConfiguration meditechSection)
        {
            services.AddOptions<MeditechAdapterOptions>().Bind(meditechSection);
            services.AddVendorAdapterTokenAcquirer();
            services.AddHttpClient("Meditech");
            services.AddSingleton<MeditechAuthProvider>();
            services.AddSingleton<IExternalEhrAuthProvider>(sp => sp.GetRequiredService<MeditechAuthProvider>());
            services.AddSingleton<MeditechFhirAdapter>();
            services.AddSingleton<IExternalEhrAdapter>(sp => sp.GetRequiredService<MeditechFhirAdapter>());
            return services;
        }
    }
}
