namespace Dialysis.Analytics.Services;

/// <summary>No-op when PublicHealth de-identification is not configured. Research export uses raw data.</summary>
public sealed class NoOpDeidentificationApiClient : IDeidentificationApiClient
{
    public Task<Stream?> DeidentifyAsync(Stream fhirJsonInput, string level = "Basic", CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(null);
}
