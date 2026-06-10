using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Tefca.Ias;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Outbound.Partners.Http;

public static class HttpPartnerServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Reads <c>Hie:Partners</c> from configuration and registers one <see cref="FhirHttpPartnerEndpoint"/>
        /// per configured partner. Each partner gets a named <see cref="HttpClient"/> so HttpClientFactory's
        /// connection pooling and lifetime management apply. Partners without a non-empty <c>BaseUrl</c> are skipped.
        /// </summary>
        public IServiceCollection AddFhirHttpPartnerEndpoints(
            IConfiguration configuration)
        {
            var partnersSection = configuration.GetSection("Hie:Partners");
            var partners = new Dictionary<string, PartnerHttpOptions>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in partnersSection.GetChildren())
            {
                var partnerId = child.Key;
                var options = child.Get<PartnerHttpOptions>();
                // Direct-transport partners are wired by AddHieDirectMessaging, not here.
                if (options is null || options.Transport == PartnerTransport.Direct || string.IsNullOrWhiteSpace(options.BaseUrl))
                    continue;
                partners[partnerId] = options;
            }

            services.AddHttpClient();

            foreach (var (partnerId, options) in partners)
            {
                var capturedId = partnerId;
                var capturedOptions = options;
                services.AddHttpClient($"hie-partner:{capturedId}");
                services.AddSingleton<IPartnerEndpoint>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var client = factory.CreateClient($"hie-partner:{capturedId}");
                    var logger = sp.GetRequiredService<ILogger<FhirHttpPartnerEndpoint>>();
                    // Optional: when the host wires an IAS issuer, IAS-enabled partners mint a
                    // per-call JWT; otherwise the static bearer token stands.
                    var iasIssuer = sp.GetService<IIasJwtIssuer>();
                    return new FhirHttpPartnerEndpoint(capturedId, client, capturedOptions, logger, iasIssuer);
                });
            }

            return services;
        }
    }
}
