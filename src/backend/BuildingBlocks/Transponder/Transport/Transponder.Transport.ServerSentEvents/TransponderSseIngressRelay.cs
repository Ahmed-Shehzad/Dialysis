using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Fans out published envelopes to every open GET <c>subscribe</c> response stream.</summary>
public sealed class TransponderSseIngressRelay(ILogger<TransponderSseIngressRelay> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();

    /// <summary>Blocks until the client disconnects.</summary>
    public async Task SubscribeAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var response = httpContext.Response;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache, no-transform";
        response.Headers.Append("X-Accel-Buffering", "no");

        await response.StartAsync(cancellationToken).ConfigureAwait(false);

        var id = Guid.NewGuid();
        var subscriber = new Subscriber(response.Body);
        _subscribers[id] = subscriber;

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (httpContext.RequestAborted.Register(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), tcs))
        {
            try
            {
                await subscriber
                    .WriteRawAsync(Encoding.UTF8.GetBytes(": transponder-sse\n\n"), cancellationToken)
                    .ConfigureAwait(false);

                await tcs.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // client or host disconnected
            }
            finally
            {
                _subscribers.TryRemove(id, out _);
            }
        }
    }

    /// <summary>Serializes <paramref name="envelope"/> as one SSE event and writes to all subscribers.</summary>
    public async Task BroadcastAsync(TransponderSseEnvelopeDto envelope, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");

        foreach (var kv in _subscribers.ToArray())
        {
            try
            {
                await kv.Value.WriteRawAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Transponder SSE: removing subscriber {Id} after write failure", kv.Key);
                _subscribers.TryRemove(kv.Key, out _);
            }
        }
    }

    private sealed class Subscriber(Stream body)
    {
        private readonly SemaphoreSlim _write = new(1, 1);

        public async Task WriteRawAsync(byte[] payload, CancellationToken cancellationToken)
        {
            await _write.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await body.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _write.Release();
            }
        }
    }
}
