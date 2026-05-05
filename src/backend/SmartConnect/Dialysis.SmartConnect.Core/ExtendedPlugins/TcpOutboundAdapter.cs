using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Sends the outbound payload over a TCP connection. Supports raw, 4-byte length-prefixed,
/// and MLLP-framed delivery (default) so this adapter can pair naturally with HL7-style listeners.
/// </summary>
/// <remarks>
/// Connection caching is opt-in via <see cref="TcpOutboundParameters.KeepConnectionOpen"/>.
/// Cached connections are pooled per <c>Host:Port</c>; serialized writes are guarded by a
/// per-key semaphore. The cache reconnects automatically on send failure.
/// </remarks>
public sealed class TcpOutboundAdapter : IOutboundAdapter, IDisposable
{
    public const string KindValue = "tcp";

    private const byte MllpStart = 0x0B;
    private const byte MllpEnd1 = 0x1C;
    private const byte MllpEnd2 = 0x0D;

    private readonly ConcurrentDictionary<string, PooledConnection> _pool = new(StringComparer.OrdinalIgnoreCase);

    public string Kind => KindValue;

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(HttpOutboundAdapter.ParametersMetadataKey, out var json) ||
            string.IsNullOrWhiteSpace(json))
        {
            return new OutboundSendResult(false, "TCP outbound requires parameters JSON with Host and Port.");
        }

        TcpOutboundParameters? opts;
        try
        {
            opts = JsonSerializer.Deserialize<TcpOutboundParameters>(json);
        }
        catch (JsonException ex)
        {
            return new OutboundSendResult(false, $"TCP outbound parameters JSON is invalid: {ex.Message}");
        }

        if (opts is null || string.IsNullOrWhiteSpace(opts.Host) || opts.Port <= 0 || opts.Port > 65535)
        {
            return new OutboundSendResult(false, "TCP outbound parameters must include Host and a valid Port.");
        }

        var framed = Frame(message.Payload, opts.Framing);

        try
        {
            if (opts.KeepConnectionOpen)
            {
                return await SendPooledAsync(opts, framed, cancellationToken).ConfigureAwait(false);
            }

            return await SendOnceAsync(opts, framed, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new OutboundSendResult(false, ex.Message);
        }
    }

    private async static Task<OutboundSendResult> SendOnceAsync(
        TcpOutboundParameters opts,
        ReadOnlyMemory<byte> framed,
        CancellationToken cancellationToken)
    {
        using var client = new TcpClient { SendTimeout = opts.SendTimeoutMs };
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(opts.ConnectTimeoutMs);
        await client.ConnectAsync(opts.Host!, opts.Port, connectCts.Token).ConfigureAwait(false);
        var stream = client.GetStream();
        await stream.WriteAsync(framed, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return new OutboundSendResult(true, null);
    }

    private async Task<OutboundSendResult> SendPooledAsync(
        TcpOutboundParameters opts,
        ReadOnlyMemory<byte> framed,
        CancellationToken cancellationToken)
    {
        var key = $"{opts.Host}:{opts.Port}";
        var pooled = _pool.GetOrAdd(key, _ => new PooledConnection());
        await pooled.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (pooled.Client is null || !pooled.Client.Connected)
                    {
                        pooled.Reset();
                        var client = new TcpClient { SendTimeout = opts.SendTimeoutMs };
                        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        connectCts.CancelAfter(opts.ConnectTimeoutMs);
                        await client.ConnectAsync(opts.Host!, opts.Port, connectCts.Token).ConfigureAwait(false);
                        pooled.Client = client;
                    }

                    var stream = pooled.Client.GetStream();
                    await stream.WriteAsync(framed, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    return new OutboundSendResult(true, null);
                }
                catch (Exception ex) when (ex is IOException or SocketException && attempt == 0)
                {
                    // drop and reconnect once
                    pooled.Reset();
                }
            }

            return new OutboundSendResult(false, "TCP outbound failed twice on a pooled connection.");
        }
        finally
        {
            pooled.Lock.Release();
        }
    }

    private static ReadOnlyMemory<byte> Frame(ReadOnlyMemory<byte> payload, TcpOutboundFraming framing)
    {
        switch (framing)
        {
            case TcpOutboundFraming.None:
                return payload;
            case TcpOutboundFraming.LengthPrefixed:
            {
                var result = new byte[4 + payload.Length];
                BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(0, 4), payload.Length);
                payload.Span.CopyTo(result.AsSpan(4));
                return result;
            }
            case TcpOutboundFraming.Mllp:
            {
                var result = new byte[1 + payload.Length + 2];
                result[0] = MllpStart;
                payload.Span.CopyTo(result.AsSpan(1, payload.Length));
                result[1 + payload.Length] = MllpEnd1;
                result[1 + payload.Length + 1] = MllpEnd2;
                return result;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(framing), framing, "Unsupported TCP framing.");
        }
    }

    public void Dispose()
    {
        foreach (var pooled in _pool.Values)
        {
            pooled.Dispose();
        }

        _pool.Clear();
    }

    private sealed class PooledConnection : IDisposable
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public TcpClient? Client { get; set; }

        public void Reset()
        {
            try { Client?.Close(); } catch { /* best effort */ }
            Client?.Dispose();
            Client = null;
        }

        public void Dispose()
        {
            Reset();
            Lock.Dispose();
        }
    }
}
