using System.Net;
using System.Net.Sockets;
using Dialysis.SmartConnect;
using Dialysis.SmartConnect.Inbound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Inbound.Mllp;

/// <summary>
/// Listens for TCP connections, parses MLLP frames, and dispatches each payload via <see cref="IInboundTransport"/>.
/// </summary>
public sealed class MllpInboundHostedService(
    IOptionsMonitor<MllpInboundOptions> options,
    IInboundMessageFactory messageFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<MllpInboundHostedService> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.CurrentValue;
        if (opt.DefaultFlowId == Guid.Empty)
        {
            logger.LogWarning("SmartConnect MLLP inbound is not listening because DefaultFlowId is empty.");
            return;
        }

        if (!IPAddress.TryParse(opt.ListenAddress, out var address))
        {
            address = IPAddress.Any;
        }

        using var listener = new TcpListener(address, opt.ListenPort);
        listener.Start();
        logger.LogInformation(
            "SmartConnect MLLP inbound listening on {Address}:{Port}, flow {FlowId}.",
            address,
            opt.ListenPort,
            opt.DefaultFlowId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    logger.LogDebug(ex, "MLLP accept failed.");
                    continue;
                }

                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var opt = options.CurrentValue;
                var decoder = new MllpFrameDecoder(opt.MaxMessageBytes);
                var ioBuffer = new byte[8192];
                while (!stoppingToken.IsCancellationRequested)
                {
                    int read;
                    try
                    {
                        read = await stream.ReadAsync(ioBuffer.AsMemory(0, ioBuffer.Length), stoppingToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "MLLP read ended.");
                        break;
                    }

                    if (read == 0)
                    {
                        break;
                    }

                    decoder.Append(ioBuffer.AsSpan(0, read));
                    while (decoder.TryTakeMessage(out var payload) && payload is not null)
                    {
                        await DispatchOneAsync(payload, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "MLLP client handler finished.");
        }
    }

    private async Task DispatchOneAsync(byte[] payload, CancellationToken ct)
    {
        var opt = options.CurrentValue;
        var message = messageFactory.Create(
            opt.DefaultFlowId,
            payload,
            PayloadFormat.Utf8Text,
            correlationId: null,
            metadata: null);

        await using var scope = scopeFactory.CreateAsyncScope();
        var transport = scope.ServiceProvider.GetRequiredService<IInboundTransport>();
        var result = await transport.DispatchAsync(message, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "MLLP dispatch failed: {Error} (suggested HTTP {Status}).",
                result.Error,
                result.SuggestedHttpStatus);
        }
    }
}
