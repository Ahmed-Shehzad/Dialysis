using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.Query;

public static class QueryServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires the outbound partner FHIR query client (pull). Binds <c>Hie:Query</c>, registers a
        /// named <see cref="HttpClient"/>, and the <see cref="IPartnerFhirQuery"/> implementation.
        /// </summary>
        public IServiceCollection AddHiePartnerQuery(IConfiguration configuration)
        {
            services.Configure<PartnerFhirQueryOptions>(configuration.GetSection(PartnerFhirQueryOptions.SectionName));
            services.AddHttpClient(PartnerFhirQueryClient.HttpClientName);
            services.AddScoped<IPartnerFhirQuery, PartnerFhirQueryClient>();
            services.AddScoped<IPartnerPatientDiscovery, PartnerPatientDiscoveryClient>();
            return services;
        }
    }
}
