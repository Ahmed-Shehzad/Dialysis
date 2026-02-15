using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Dialysis.ApiClients;

/// <summary>Creates IFhirApi clients for a given base URL. Used when base URL varies per tenant or request.</summary>
public interface IFhirApiFactory
{
    IFhirApi ForBaseUrl(string baseUrl);
}

public sealed class FhirApiFactory : IFhirApiFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FhirApiFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IFhirApi ForBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));
        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = baseUri;
        return RestService.For<IFhirApi>(client);
    }
}
