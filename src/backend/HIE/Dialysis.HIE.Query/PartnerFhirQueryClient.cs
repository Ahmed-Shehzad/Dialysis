using System.Net.Http.Headers;
using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Tefca.Ias;
using Dialysis.HIE.Tefca.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Dialysis.HIE.Query;

/// <summary>
/// Outbound FHIR query against a partner QHIN — the pull side of health-information exchange. Resolves
/// the destination from the <see cref="QhinPartner"/> aggregate, authenticates with a purpose-scoped
/// TEFCA IAS JWT (the same primitive Directed Exchange uses), GETs the relative query, and returns the
/// FHIR resources. Results are handed to the existing inbound ingestion pipeline by the caller.
/// </summary>
public interface IPartnerFhirQuery
{
    /// <summary>
    /// Runs <paramref name="relativeQuery"/> (e.g. <c>Patient/123/$everything</c> or
    /// <c>Observation?patient=123</c>) against <paramref name="partnerId"/>'s FHIR base URL under
    /// <paramref name="purposeOfUse"/>, with <paramref name="subject"/> as the IAS JWT subject.
    /// </summary>
    Task<IReadOnlyList<Resource>> QueryAsync(Guid partnerId, string relativeQuery, string subject, string purposeOfUse, CancellationToken cancellationToken = default);
}

public sealed class PartnerFhirQueryClient : IPartnerFhirQuery
{
    /// <summary>Named <see cref="HttpClient"/> for partner pull requests.</summary>
    public const string HttpClientName = "hie-query";

    private static readonly FhirJsonDeserializer _parser =
        new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIasJwtIssuer _iasJwtIssuer;
    private readonly IQhinPartnerRepository _partners;
    private readonly PartnerFhirQueryOptions _options;
    private readonly ILogger<PartnerFhirQueryClient> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public PartnerFhirQueryClient(
        IHttpClientFactory httpClientFactory,
        IIasJwtIssuer iasJwtIssuer,
        IQhinPartnerRepository partners,
        IOptions<PartnerFhirQueryOptions> options,
        ILogger<PartnerFhirQueryClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _iasJwtIssuer = iasJwtIssuer;
        _partners = partners;
        _options = options.Value;
        _logger = logger;
        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
            })
            .Build();
    }

    public async Task<IReadOnlyList<Resource>> QueryAsync(Guid partnerId, string relativeQuery, string subject, string purposeOfUse, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeQuery);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(purposeOfUse);

        var partner = await _partners.FindAsync(partnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{partnerId}' not found.");
        if (partner.Status != QhinPartnerStatus.Active)
            throw new InvalidOperationException($"QHIN partner '{partner.Name}' is not Active ({partner.Status}); refusing to query.");
        if (!partner.IsPurposePermitted(purposeOfUse))
            throw new InvalidOperationException($"QHIN partner '{partner.Name}' does not permit purpose '{purposeOfUse}'.");

        var token = _iasJwtIssuer.Issue(new IasJwtRequest(
            Issuer: _options.IasIssuer,
            Audience: partner.IasEndpoint,
            Subject: subject,
            Scope: _options.IasScope,
            Lifetime: TimeSpan.FromSeconds(_options.IasLifetimeSeconds),
            PurposeOfUse: purposeOfUse));

        using var client = _httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri(partner.FhirBaseUrl.EndsWith('/') ? partner.FhirBaseUrl : partner.FhirBaseUrl + "/");
        if (_options.TimeoutSeconds > 0)
            client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var response = await _pipeline.ExecuteAsync(async ct =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeQuery.TrimStart('/'));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
            return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Partner {PartnerId} query '{Query}' returned {Status}", partnerId, relativeQuery, (int)response.StatusCode);
                throw new InvalidOperationException($"Partner query failed: HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Parse(body);
        }
    }

    // A search/$everything response is a Bundle; a read is a single resource. Return resources either way.
    private List<Resource> Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return [];
        var resource = _parser.Deserialize<Resource>(body);
        if (resource is Bundle bundle)
            return bundle.Entry.Select(e => e.Resource).Where(r => r is not null).Cast<Resource>().ToList();
        return [resource];
    }
}
