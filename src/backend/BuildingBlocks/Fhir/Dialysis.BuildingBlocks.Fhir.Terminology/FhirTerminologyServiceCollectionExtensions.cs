using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

public static class FhirTerminologyServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory <see cref="ITerminologyService"/> backed by user-supplied CodeSystems /
    /// ValueSets / ConceptMaps. Use this in test fixtures and modules that ship their own bundled
    /// terminology (e.g. <c>$expand</c> on a small US Core value set without an upstream server).
    /// </summary>
    public static IServiceCollection AddInMemoryFhirTerminology(
        this IServiceCollection services,
        Action<InMemoryTerminologyService>? configure = null)
    {
        var service = new InMemoryTerminologyService();
        configure?.Invoke(service);
        services.TryAddSingleton<ITerminologyService>(service);
        services.TryAddSingleton<IUcumService, UcumService>();
        return services;
    }

    /// <summary>
    /// Registers the production HTTP-backed <see cref="ITerminologyService"/> with Polly retry +
    /// <see cref="MemoryCacheTerminologyDecorator"/> in front. Reads <see cref="FhirTerminologyOptions"/>
    /// from <paramref name="configSectionPath"/> (default <c>Fhir:Terminology</c>) and falls back to
    /// <see cref="FhirTerminologyOptions.TxFhirOrgR4"/> when the endpoint is not configured.
    /// </summary>
    public static IServiceCollection AddFhirTerminology(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionPath = "Fhir:Terminology")
    {
        services.AddOptions<FhirTerminologyOptions>()
            .Bind(configuration.GetSection(configSectionPath))
            .PostConfigure(o =>
            {
                if (string.IsNullOrWhiteSpace(o.Endpoint))
                    o.Endpoint = FhirTerminologyOptions.TxFhirOrgR4;
            });

        services.TryAddSingleton<IUcumService, UcumService>();

        services.AddMemoryCache(o =>
        {
            using var sp = services.BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<FhirTerminologyOptions>>().Value;
            if (opts.CacheSizeLimit > 0)
                o.SizeLimit = opts.CacheSizeLimit;
        });

        services
            .AddHttpClient<HttpFhirTerminologyService>(HttpFhirTerminologyService.HttpClientName)
            .AddPolicyHandler((sp, _) =>
            {
                var opts = sp.GetRequiredService<IOptions<FhirTerminologyOptions>>().Value;
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        opts.RetryCount,
                        attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
            });

        services.AddSingleton<ITerminologyService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient(HttpFhirTerminologyService.HttpClientName);
            var inner = new HttpFhirTerminologyService(
                http,
                sp.GetRequiredService<IOptions<FhirTerminologyOptions>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HttpFhirTerminologyService>>());

            return new MemoryCacheTerminologyDecorator(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptions<FhirTerminologyOptions>>());
        });

        return services;
    }
}
