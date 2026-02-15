using Dialysis.ApiClients;
using Dialysis.Analytics.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.Analytics.Services;

/// <summary>De-identification via Refit IPublicHealthDeidentifyApi.</summary>
public sealed class RefitDeidentificationApiClient : IDeidentificationApiClient
{
    private readonly IPublicHealthDeidentifyApi _api;
    private readonly AnalyticsOptions _options;

    public RefitDeidentificationApiClient(IPublicHealthDeidentifyApi api, IOptions<AnalyticsOptions> options)
    {
        _api = api;
        _options = options.Value;
    }

    public async Task<Stream?> DeidentifyAsync(Stream fhirJsonInput, string level = "Basic", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicHealthBaseUrl)) return null;

        try
        {
            return await _api.DeidentifyAsync(fhirJsonInput, "application/fhir+json", level, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
