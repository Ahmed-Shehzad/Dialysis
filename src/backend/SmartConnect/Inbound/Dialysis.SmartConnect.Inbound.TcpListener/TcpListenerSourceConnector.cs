using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Inbound.TcpListener;

/// <summary>
/// Generic TCP listener source connector with pluggable frame decoding (None, LF, MLLP, LengthPrefixed).
/// </summary>
public sealed class TcpListenerSourceConnector : ISourceConnector
{
    public const string KindValue = "tcp-listener";

    public string Kind => KindValue;

    public async Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        TcpListenerParameters parameters;
        try
        {
            parameters = TcpListenerParameters.Parse(context.Parameters);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError(ex, "TcpListener '{Name}' has invalid parameters; not starting.", context.InstanceName);
            return;
        }

        var endpoint = new IPEndPoint(IPAddress.Parse(parameters.ListenAddress), parameters.ListenPort);
        var listener = new System.Net.Sockets.TcpListener(endpoint);
        listener.Start();

        context.Logger.LogInformation(
            "TcpListener '{Name}' listening on {Endpoint} (framing={Framing}, maxConns={MaxConns}).",
            context.InstanceName,
            endpoint,
            parameters.Framing,
            parameters.MaxConnections);

        using var semaphore = new SemaphoreSlim(parameters.MaxConnections, parameters.MaxConnections);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                Socket socket;
                try
                {
                    socket = await listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    semaphore.Release();
                    break;
                }

                _ = HandleConnectionAsync(socket, context, parameters, semaphore, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task HandleConnectionAsync(
        Socket socket,
        SourceConnectorContext context,
        TcpListenerParameters parameters,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new NetworkStream(socket, ownsSocket: true);
            var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: 4096));

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                while (TryReadFrame(ref buffer, parameters.Framing, parameters.MaxMessageBytes, out var frame))
                {
                    var payload = frame.ToArray();
                    var msg = context.MessageFactory.Create(
                        context.DefaultFlowId,
                        payload,
                        PayloadFormat.Utf8Text,
                        correlationId: null);

                    await context.DispatchAsync(msg, cancellationToken).ConfigureAwait(false);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await reader.CompleteAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "TcpListener '{Name}' connection error.", context.InstanceName);
        }
        finally
        {
            semaphore.Release();
        }
    }

    internal static bool TryReadFrame(
        ref ReadOnlySequence<byte> buffer,
        FrameDecodingMode framing,
        int maxBytes,
        out ReadOnlySequence<byte> frame)
    {
        frame = default;

        if (buffer.IsEmpty)
        {
            return false;
        }

        switch (framing)
        {
            case FrameDecodingMode.None:
                var length = (int)Math.Min(buffer.Length, maxBytes);
                frame = buffer.Slice(0, length);
                buffer = buffer.Slice(frame.End);
                return true;

            case FrameDecodingMode.LineFeed:
                var lfPos = buffer.PositionOf((byte)'\n');
                if (lfPos == null)
                {
                    return false;
                }

                frame = buffer.Slice(0, lfPos.Value);
                buffer = buffer.Slice(buffer.GetPosition(1, lfPos.Value));
                return true;

            case FrameDecodingMode.Mllp:
                const byte sb = 0x0B;
                const byte eb = 0x1C;
                const byte cr = 0x0D;

                var sbPos = buffer.PositionOf(sb);
                if (sbPos == null)
                {
                    return false;
                }

                var afterSb = buffer.Slice(buffer.GetPosition(1, sbPos.Value));
                var ebPos = afterSb.PositionOf(eb);
                if (ebPos == null)
                {
                    return false;
                }

                // Verify CR follows EB
                var afterEb = afterSb.Slice(afterSb.GetPosition(1, ebPos.Value));
                if (afterEb.IsEmpty)
                {
                    return false;
                }

                var firstAfterEb = afterEb.FirstSpan[0];
                if (firstAfterEb != cr)
                {
                    return false;
                }

                frame = afterSb.Slice(0, ebPos.Value);
                buffer = afterEb.Slice(1);
                return true;

            case FrameDecodingMode.LengthPrefixed:
                if (buffer.Length < 4)
                {
                    return false;
                }

                Span<byte> lenBytes = stackalloc byte[4];
                buffer.Slice(0, 4).CopyTo(lenBytes);
                var msgLen = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(lenBytes);
                if (msgLen > maxBytes)
                {
                    // skip malformed
                    buffer = buffer.Slice(4);
                    return false;
                }

                if (buffer.Length < 4 + msgLen)
                {
                    return false;
                }

                frame = buffer.Slice(4, msgLen);
                buffer = buffer.Slice(4 + msgLen);
                return true;

            default:
                return false;
        }
    }
}
