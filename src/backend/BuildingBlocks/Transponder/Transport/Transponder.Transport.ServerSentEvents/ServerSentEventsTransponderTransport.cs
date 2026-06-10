using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>
/// HTTP client: POST JSON to publish, GET <c>text/event-stream</c> for subscribe (see <see cref="TransponderSseServerExtensions"/>).
/// </summary>
public sealed class ServerSentEventsTransponderTransport : ITransponderTransport
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private HttpClient? _http;
    private readonly IOptions<TransponderSseClientOptions> _options;
    private readonly ILogger<ServerSentEventsTransponderTransport> _logger;
    /// <summary>
    /// HTTP client: POST JSON to publish, GET <c>text/event-stream</c> for subscribe (see <see cref="TransponderSseServerExtensions"/>).
    /// </summary>
    public ServerSentEventsTransponderTransport(IOptions<TransponderSseClientOptions> options,
        ILogger<ServerSentEventsTransponderTransport> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_http is not null)
                return;

            var o = _options.Value;
            if (string.IsNullOrWhiteSpace(o.BaseAddress))
                throw new InvalidOperationException("Transponder SSE: BaseAddress is required (e.g. https://localhost:5001/).");

            var client = new HttpClient { BaseAddress = new Uri(o.BaseAddress.TrimEnd('/') + "/", UriKind.Absolute) };
            _http = client;
            _logger.LogInformation("Transponder SSE client base address {BaseAddress}", client.BaseAddress);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var http = _http ?? throw new InvalidOperationException("HTTP client is not initialized.");
        var o = _options.Value;

        var dto = ToDto(message);
        using var request = new HttpRequestMessage(HttpMethod.Post, o.PublishPath.TrimStart('/'));
        request.Content = JsonContent.Create(dto, options: _jsonOptions);
        await ApplyAuthAsync(request).ConfigureAwait(false);

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var http = _http ?? throw new InvalidOperationException("HTTP client is not initialized.");
        var o = _options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Get, o.SubscribePath.TrimStart('/'));
        await ApplyAuthAsync(request).ConfigureAwait(false);

        using var response = await http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);

        var dataLines = new List<string>();
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                if (dataLines.Count > 0)
                {
                    var last = TryParseEnvelope(string.Join('\n', dataLines));
                    if (last is not null)
                        await onMessage(last.Value, cancellationToken).ConfigureAwait(false);
                }

                break;
            }

            if (line.Length == 0)
            {
                if (dataLines.Count > 0)
                {
                    var msg = TryParseEnvelope(string.Join('\n', dataLines));
                    if (msg is not null)
                        await onMessage(msg.Value, cancellationToken).ConfigureAwait(false);
                    dataLines.Clear();
                }

                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
                dataLines.Add(line[5..].TrimStart());
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_http is not null)
            {
                _http.Dispose();
                _http = null;
            }
        }
        finally
        {
            _lifecycle.Release();
            _lifecycle.Dispose();
        }
    }

    private async Task ApplyAuthAsync(HttpRequestMessage request)
    {
        var provider = _options.Value.AccessTokenProvider;
        if (provider is null)
            return;

        var token = await provider().ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private TransportMessage? TryParseEnvelope(string json)
    {
        TransponderSseEnvelopeDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TransponderSseEnvelopeDto>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Transponder SSE: invalid JSON in event");
            return null;
        }

        if (dto is null || string.IsNullOrEmpty(dto.RoutingKey))
            return null;

        IReadOnlyDictionary<string, string>? headers = dto.Headers is { Count: > 0 }
            ? new Dictionary<string, string>(dto.Headers, StringComparer.Ordinal)
            : null;

        return new TransportMessage(
            dto.RoutingKey,
            dto.Payload,
            dto.CorrelationId,
            dto.ContentType ?? "application/json",
            DeduplicationId: dto.DeduplicationId,
            headers);
    }

    private static TransponderSseEnvelopeDto ToDto(TransportMessage message) => new()
    {
        RoutingKey = message.RoutingKey,
        Payload = message.Payload.ToArray(),
        CorrelationId = message.CorrelationId,
        DeduplicationId = message.DeduplicationId,
        ContentType = message.ContentType,
        Headers = message.Headers is { Count: > 0 }
            ? new Dictionary<string, string>(message.Headers, StringComparer.Ordinal)
            : null,
    };
}
