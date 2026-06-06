using System.Net.Http.Headers;

namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Attaches the client-credentials bearer token to every outbound driver request.</summary>
public sealed class SimulationBearerTokenHandler : DelegatingHandler
{
    private readonly IClientCredentialsTokenProvider _tokenProvider;

    /// <summary>Creates the handler.</summary>
    public SimulationBearerTokenHandler(IClientCredentialsTokenProvider tokenProvider) => _tokenProvider = tokenProvider;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
