using System.Net.Http.Headers;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>
/// Proxies FHIR terminology operations (<c>$lookup</c>, <c>$validate-code</c>, <c>$translate</c>,
/// <c>$expand</c>) to an upstream FHIR R4 terminology server (default: HL7 tx.fhir.org, prod: a self-hosted
/// Snowstorm for SNOMED CT + HAPI FHIR / Ontoserver for LOINC and friends).
///
/// Outputs are parsed Firely model objects so callers (mappers, validators) keep their existing API surface.
/// </summary>
public sealed class HttpFhirTerminologyService : ITerminologyService
{
    public const string HttpClientName = "Dialysis.Fhir.Terminology";

    private static readonly FhirJsonDeserializer _parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    private readonly HttpClient _http;
    private readonly ILogger<HttpFhirTerminologyService> _logger;

    public HttpFhirTerminologyService(
        HttpClient http,
        IOptions<FhirTerminologyOptions> options,
        ILogger<HttpFhirTerminologyService> logger)
    {
        _http = http;
        var fhirTerminologyOptions = options.Value;
        _logger = logger;

        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(fhirTerminologyOptions.Endpoint.TrimEnd('/') + "/");
        if (_http.Timeout == TimeSpan.FromSeconds(100)) // System.Net.Http default
            _http.Timeout = fhirTerminologyOptions.Timeout;

        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
        if (!string.IsNullOrWhiteSpace(fhirTerminologyOptions.BearerToken))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fhirTerminologyOptions.BearerToken);
    }

    public async ValueTask<Parameters> LookupAsync(string system, string code, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var query = $"CodeSystem/$lookup?system={Uri.EscapeDataString(system)}&code={Uri.EscapeDataString(code)}";
        return await GetFhirAsync<Parameters>(query, cancellationToken).ConfigureAwait(false) ?? new Parameters();
    }

    public async ValueTask<Parameters> ValidateCodeAsync(string valueSetUrl, string code, string? system, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valueSetUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var query = $"ValueSet/$validate-code?url={Uri.EscapeDataString(valueSetUrl)}&code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrWhiteSpace(system))
            query += $"&system={Uri.EscapeDataString(system)}";

        return await GetFhirAsync<Parameters>(query, cancellationToken).ConfigureAwait(false) ?? new Parameters();
    }

    public async ValueTask<Parameters> TranslateAsync(string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conceptMapUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSystem);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCode);

        var query = $"ConceptMap/$translate?url={Uri.EscapeDataString(conceptMapUrl)}" +
                    $"&system={Uri.EscapeDataString(sourceSystem)}&code={Uri.EscapeDataString(sourceCode)}";

        return await GetFhirAsync<Parameters>(query, cancellationToken).ConfigureAwait(false) ?? new Parameters();
    }

    public async ValueTask<ValueSet> ExpandAsync(string valueSetUrl, IReadOnlyDictionary<string, string> filters, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valueSetUrl);

        var query = new StringBuilder($"ValueSet/$expand?url={Uri.EscapeDataString(valueSetUrl)}");
        foreach (var pair in filters)
        {
            query.Append($"&{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}");
        }

        return await GetFhirAsync<ValueSet>(query.ToString(), cancellationToken).ConfigureAwait(false)
            ?? new ValueSet { Url = valueSetUrl };
    }

    private async Task<TResource?> GetFhirAsync<TResource>(string relativeUrl, CancellationToken cancellationToken)
        where TResource : Resource
    {
        using var response = await _http.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "FHIR terminology call {Url} failed: {Status} — body preview: {Preview}",
                relativeUrl, (int)response.StatusCode, Truncate(body, 256));
            return null;
        }

        try
        {
            return _parser.Deserialize<TResource>(body);
        }
        catch (Exception ex) when (ex is FormatException or DeserializationFailedException or System.Text.Json.JsonException)
        {
            _logger.LogError(ex, "Failed to parse FHIR response from {Url}", relativeUrl);
            return null;
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
