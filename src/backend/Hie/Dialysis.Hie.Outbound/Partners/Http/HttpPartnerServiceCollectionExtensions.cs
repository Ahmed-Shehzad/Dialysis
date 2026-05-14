using Dialysis.Hie.Core.Abstraction.Partners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.Hie.Outbound.Partners.Http;

public static class HttpPartnerServiceCollectionExtensions
{
    /// <summary>
    /// Reads <c>Hie:Partners</c> from configuration and registers one <see cref="FhirHttpPartnerEndpoint"/>
    /// per configured partner. Each partner gets a named <see cref="HttpClient"/> so HttpClientFactory's
    /// connection pooling and lifetime management apply. Partners without a non-empty <c>BaseUrl</c> are skipped.
    /// </summary>
    public static IServiceCollection AddFhirHttpPartnerEndpoints(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var partnersSection = configuration.GetSection("Hie:Partners");
        var partners = new Dictionary<string, PartnerHttpOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in partnersSection.GetChildren())
        {
            var partnerId = child.Key;
            var options = child.Get<PartnerHttpOptions>();
            if (options is null || string.IsNullOrWhiteSpace(options.BaseUrl)) continue;
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
                return new FhirHttpPartnerEndpoint(capturedId, client, capturedOptions, logger);
            });
        }

        return services;
    }
}
