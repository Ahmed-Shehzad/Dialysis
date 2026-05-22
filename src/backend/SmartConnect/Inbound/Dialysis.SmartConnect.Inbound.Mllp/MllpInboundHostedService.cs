using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.TimeSync;
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
    IClockSkewMonitor clockSkewMonitor,
    IClockSkewCorrectionEventSink clockSkewSink,
    TimeProvider timeProvider,
    ILogger<MllpInboundHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
        var sourceMap = ParseHl7Msh(payload);
        IReadOnlyDictionary<string, string>? metadata = null;
        if (sourceMap.Count > 0)
        {
            metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["smartconnect.sourcemap.json"] = JsonSerializer.Serialize(sourceMap),
            };
        }

        // Slice J3: feed the §2 clock-skew monitor + apply the configured correction
        // policy. Failure to parse MSH-7 / non-HL7v2 payloads silently skip; we never
        // want time-sync probing to block the dispatch path.
        await TryProbeClockSkewAsync(payload, ct).ConfigureAwait(false);

        var message = messageFactory.Create(
            opt.DefaultFlowId,
            payload,
            PayloadFormat.Utf8Text,
            correlationId: null,
            metadata: metadata);

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

    private static Dictionary<string, object?> ParseHl7Msh(byte[] payload)
    {
        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (payload.Length < 4) return map;

        var text = Encoding.UTF8.GetString(payload);
        if (!text.StartsWith("MSH", StringComparison.Ordinal)) return map;

        var segEnd = text.IndexOfAny(['\r', '\n']);
        var mshSegment = segEnd > 0 ? text[..segEnd] : text;
        // Field separator is the 4th char (after "MSH"), conventionally '|'.
        var sep = mshSegment.Length >= 4 ? mshSegment[3] : '|';
        var fields = mshSegment.Split(sep);

        // HL7 field indexing in MSH: fields[0]="MSH", fields[1] is encoding chars, then 1-based MSH-2..
        // MSH-3 sending app, MSH-4 sending facility, MSH-9 message type, MSH-10 control id, MSH-7 timestamp.
        if (fields.Length > 2 && !string.IsNullOrWhiteSpace(fields[2]))
            map["hl7.sendingApplication"] = fields[2];
        if (fields.Length > 3 && !string.IsNullOrWhiteSpace(fields[3]))
            map["hl7.sendingFacility"] = fields[3];
        if (fields.Length > 6 && !string.IsNullOrWhiteSpace(fields[6]))
            map["hl7.timestamp"] = fields[6];
        if (fields.Length > 8 && !string.IsNullOrWhiteSpace(fields[8]))
            map["hl7.messageType"] = fields[8];
        if (fields.Length > 9 && !string.IsNullOrWhiteSpace(fields[9]))
            map["hl7.controlId"] = fields[9];

        return map;
    }

    /// <summary>
    /// Slice J3 hook: parse the payload as HL7v2, compute the skew vs. server clock,
    /// record it on <see cref="IClockSkewMonitor"/>, optionally rewrite MSH-7 per the
    /// configured policy, and publish an audit event when a correction fires. Any parse
    /// failure is swallowed so a non-HL7v2 inbound (e.g. an exception trace coming in
    /// through the MLLP socket) never blocks dispatch.
    /// </summary>
    private async Task TryProbeClockSkewAsync(byte[] payload, CancellationToken cancellationToken)
    {
        try
        {
            var text = Encoding.UTF8.GetString(payload);
            var message = Hl7V2Message.Parse(text);
            var policy = ResolveClockSkewPolicy();
            var result = Hl7V2ClockSkewProbe.TryObserveAndCorrect(
                message, timeProvider.GetUtcNow().UtcDateTime, clockSkewMonitor, policy);
            if (result is { WasCorrected: true })
            {
                await clockSkewSink.PublishAsync(result, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (FormatException)
        {
            // Not an HL7v2 message — fine, the probe is best-effort.
        }
        catch (ArgumentException)
        {
            // Empty / whitespace payload — ignore.
        }
    }

    private ClockSkewCorrectionPolicy ResolveClockSkewPolicy()
    {
        var opt = options.CurrentValue.ClockSkew;
        if (string.Equals(opt.Mode, "Normalize", StringComparison.OrdinalIgnoreCase))
        {
            return ClockSkewCorrectionPolicy.Normalize(
                correctAbove: TimeSpan.FromSeconds(opt.CorrectAboveAbsSkewSeconds),
                maxAllowed: TimeSpan.FromSeconds(opt.MaxAllowedAbsJumpSeconds));
        }
        return ClockSkewCorrectionPolicy.ReportOnly;
    }
}
