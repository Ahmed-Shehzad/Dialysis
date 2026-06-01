using System.Net.Http.Json;
using Dialysis.BuildingBlocks.Tefca.Ti.Endpoints;
using Dialysis.BuildingBlocks.Tefca.Ti.Smcb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Tefca.Ti;

/// <summary>
/// Default <see cref="ITelematikInfrastrukturClient"/> implementation. Wraps an
/// HttpClient (which the host's <c>MutualTlsHttpClientFactory</c> configures with the
/// gematik trust-anchor pack); reads the SMC-B chain via <see cref="ISmcBCardReader"/> for
/// the handshake; never holds long-lived credentials in memory.
/// </summary>
public sealed class GematikTelematikInfrastrukturClient : ITelematikInfrastrukturClient
{
    private readonly HttpClient _httpClient;
    private readonly ISmcBCardReader _smcb;
    private readonly TiOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<GematikTelematikInfrastrukturClient> _logger;

    public GematikTelematikInfrastrukturClient(
        HttpClient httpClient,
        ISmcBCardReader smcb,
        IOptions<TiOptions> options,
        TimeProvider clock,
        ILogger<GematikTelematikInfrastrukturClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(smcb);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _smcb = smcb;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public GematikEnvironment Environment => _options.Environment;

    public async Task<TiHandshakeResult> HandshakeAsync(CancellationToken cancellationToken)
    {
        var endpoint = GematikEndpointCatalog.For(_options.Environment);
        try
        {
            // PU (Produktion) requires an explicit production-mode opt-in AND a real SMC-B
            // present. RU / TU work without a card for conformance testing.
            if (_options.Environment == GematikEnvironment.Produktion && !_options.ProductionModeOptIn)
            {
                return new TiHandshakeResult(
                    Succeeded: false,
                    CheckedAtUtc: _clock.GetUtcNow(),
                    FailureReason: "TI Produktion mode is not opted-in; set DataProtection:TiProductionMode=true.",
                    IdpIssuer: null,
                    SmcBChainFingerprintSha256: null);
            }

            using var resp = await _httpClient
                .GetAsync(endpoint.DiscoveryDocument, cancellationToken)
                .ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new TiHandshakeResult(
                    Succeeded: false,
                    CheckedAtUtc: _clock.GetUtcNow(),
                    FailureReason: $"gematik IDP discovery returned {(int)resp.StatusCode}.",
                    IdpIssuer: null,
                    SmcBChainFingerprintSha256: null);
            }

            var discovery = await resp.Content
                .ReadFromJsonAsync<GematikDiscoveryDocument>(cancellationToken)
                .ConfigureAwait(false);

            string? chainHash = null;
            if (_smcb.IsPresent)
            {
                var chain = await _smcb.ReadCertificateChainAsync(cancellationToken).ConfigureAwait(false);
                chainHash = chain.ChainFingerprintSha256;
            }

            return new TiHandshakeResult(
                Succeeded: true,
                CheckedAtUtc: _clock.GetUtcNow(),
                FailureReason: null,
                IdpIssuer: discovery?.Issuer,
                SmcBChainFingerprintSha256: chainHash);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "gematik TI handshake failed for environment {Env}.", _options.Environment);
            return new TiHandshakeResult(
                Succeeded: false,
                CheckedAtUtc: _clock.GetUtcNow(),
                FailureReason: ex.Message,
                IdpIssuer: null,
                SmcBChainFingerprintSha256: null);
        }
    }

    private sealed record GematikDiscoveryDocument
    {
        [System.Text.Json.Serialization.JsonPropertyName("issuer")]
        public string? Issuer { get; init; }
    }
}

public sealed class TiOptions
{
    public GematikEnvironment Environment { get; set; } = GematikEnvironment.Test;

    /// <summary>Explicit opt-in for Produktion mode. Refused without it.</summary>
    public bool ProductionModeOptIn { get; set; }
}
