using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Dialysis.HIE.Core.Abstraction.Partners;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Dialysis.HIE.Outbound.Partners.Http;

/// <summary>
/// Delivers FHIR resources to an external partner over HTTPS using <c>application/fhir+json</c>.
/// Retries on transient failures (network, 5xx, 408, 429) with exponential backoff.
/// </summary>
public sealed class FhirHttpPartnerEndpoint : IPartnerEndpoint
{
    private static readonly FhirJsonSerializer _serializer = new();

    private readonly HttpClient _httpClient;
    private readonly PartnerHttpOptions _options;
    private readonly ILogger<FhirHttpPartnerEndpoint> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public FhirHttpPartnerEndpoint(
        string partnerId,
        HttpClient httpClient,
        PartnerHttpOptions options,
        ILogger<FhirHttpPartnerEndpoint> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        PartnerId = partnerId;
        _httpClient = httpClient;
        _options = options;
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

    public async Task<PartnerDeliveryResult> DeliverAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var json = await _serializer.SerializeToStringAsync(resource).ConfigureAwait(false);
        var path = string.IsNullOrWhiteSpace(resource.Id)
            ? resource.TypeName
            : $"{resource.TypeName}/{resource.Id}";

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
                if (!string.IsNullOrWhiteSpace(_options.BearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);

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

    private static bool IsTransient(HttpStatusCode status) =>
        (int)status >= 500
        || status == HttpStatusCode.RequestTimeout
        || status == HttpStatusCode.TooManyRequests;
}
