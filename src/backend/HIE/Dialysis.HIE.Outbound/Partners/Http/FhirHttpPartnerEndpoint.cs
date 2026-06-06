using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Tefca.Ias;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Dialysis.HIE.Outbound.Partners.Http;

/// <summary>
/// Delivers FHIR resources to an external partner over HTTPS using <c>application/fhir+json</c>.
/// Retries on transient failures (network, 5xx, 408, 429) with exponential backoff. Authenticates
/// with a per-call, patient- and purpose-scoped TEFCA IAS JWT when the partner opts in
/// (<see cref="PartnerHttpOptions.UseIasJwt"/>), else a static bearer token.
/// </summary>
public sealed class FhirHttpPartnerEndpoint : IPartnerEndpoint
{
    // ToJson is CPU-only; calling it from a non-Async method keeps VSTHRD103 quiet.
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    private readonly HttpClient _httpClient;
    private readonly PartnerHttpOptions _options;
    private readonly IIasJwtIssuer? _iasJwtIssuer;
    private readonly ILogger<FhirHttpPartnerEndpoint> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public FhirHttpPartnerEndpoint(
        string partnerId,
        HttpClient httpClient,
        PartnerHttpOptions options,
        ILogger<FhirHttpPartnerEndpoint> logger,
        IIasJwtIssuer? iasJwtIssuer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        PartnerId = partnerId;
        _httpClient = httpClient;
        _options = options;
        _iasJwtIssuer = iasJwtIssuer;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        if (_options.TimeoutSeconds > 0)
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

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
                    .HandleResult(r => IsTransient(r.StatusCode)),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry {Attempt} for partner {PartnerId} after {Delay}ms (status {Status})",
                        args.AttemptNumber + 1, partnerId, args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    public string PartnerId { get; }

    public async Task<PartnerDeliveryResult> DeliverAsync(Resource resource, PartnerDeliveryContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var json = SerializeFhirJson(resource);
        var path = string.IsNullOrWhiteSpace(resource.Id)
            ? resource.TypeName
            : $"{resource.TypeName}/{resource.Id}";

        // One token per delivery (valid for the whole retry budget); patient + purpose scoped.
        var authToken = ResolveAuthorizationToken(context);

        HttpResponseMessage response;
        try
        {
            response = await _pipeline.ExecuteAsync(async ct =>
            {
                using var request = new HttpRequestMessage(
                    string.IsNullOrWhiteSpace(resource.Id) ? HttpMethod.Post : HttpMethod.Put,
                    path);
                request.Content = new StringContent(json, Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/fhir+json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
                if (!string.IsNullOrWhiteSpace(authToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new PartnerDeliveryResult(false, 0, ex.Message);
        }

        try
        {
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
                return new PartnerDeliveryResult(true, statusCode, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var truncated = body.Length > 512 ? body[..512] + "…" : body;
            return new PartnerDeliveryResult(false, statusCode, $"{response.ReasonPhrase}: {truncated}");
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Picks the bearer credential for the call: a freshly minted, patient- and purpose-scoped
    /// TEFCA IAS JWT when the partner opts in and an issuer is wired; otherwise the static token.
    /// A minting failure (e.g. unconfigured signing key) falls back to the static token so a
    /// half-configured IAS partner still delivers under its legacy credential rather than hard-failing.
    /// </summary>
    private string? ResolveAuthorizationToken(PartnerDeliveryContext context)
    {
        if (!_options.UseIasJwt)
            return _options.BearerToken;

        if (_iasJwtIssuer is null)
        {
            _logger.LogWarning(
                "Partner {PartnerId} is configured for IAS JWT but no issuer is wired; falling back to static token.",
                PartnerId);
            return _options.BearerToken;
        }

        try
        {
            return _iasJwtIssuer.Issue(new IasJwtRequest(
                Issuer: _options.IasIssuer,
                Audience: string.IsNullOrWhiteSpace(_options.IasAudience) ? _options.BaseUrl : _options.IasAudience,
                Subject: context.PatientId.ToString(),
                Scope: _options.IasScope,
                Lifetime: TimeSpan.FromSeconds(_options.IasLifetimeSeconds),
                PurposeOfUse: context.PurposeOfUse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to mint IAS JWT for partner {PartnerId}; falling back to static token.", PartnerId);
            return _options.BearerToken;
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        (int)status >= 500
        || status == HttpStatusCode.RequestTimeout
        || status == HttpStatusCode.TooManyRequests;
}
