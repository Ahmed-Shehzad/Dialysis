using System.Net;
using System.Net.Sockets;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.Mllp;
using Dialysis.SmartConnect.TimeSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Integration;

/// <summary>
/// Real-socket end-to-end: binds <see cref="MllpInboundHostedService"/> on an ephemeral port,
/// opens a real TCP client, sends an MLLP-framed HL7 payload, and asserts the captured payload
/// at the inbound transport equals the bytes between 0x0B and 0x1C 0x0D. Marked
/// <c>RealSocket</c> so it can be filtered on flaky runners (the default <c>dotnet test</c>
/// still picks it up — no infra needed beyond loopback).
/// </summary>
[Trait("Category", "RealSocket")]
public sealed class MllpRealSocketEndToEndTests
{
    private const byte StartBlock = 0x0B;
    private const byte EndBlock = 0x1C;
    private const byte CarriageReturn = 0x0D;

    [Fact]
    public async Task Mllp_Source_Forwards_Framed_Payload_To_Inbound_Transport_Async()
    {
        var port = FindFreePort();
        var options = new MllpInboundOptions
        {
            ListenAddress = "127.0.0.1",
            ListenPort = port,
            DefaultFlowId = Guid.NewGuid(),
        };
        var capturedTransport = new CapturingInboundTransport();
        var services = new ServiceCollection();
        services.AddSingleton<IInboundTransport>(capturedTransport);
        await using var provider = services.BuildServiceProvider();

        var hosted = new MllpInboundHostedService(
            new TestOptionsMonitor<MllpInboundOptions>(options),
            new PassThroughInboundMessageFactory(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            new NoOpClockSkewMonitor(),
            new NoOpClockSkewCorrectionEventSink(),
            TimeProvider.System,
            NullLogger<MllpInboundHostedService>.Instance);

        var cts = new CancellationTokenSource();
        await hosted.StartAsync(cts.Token);
        await Task.Delay(500); // bind settle

        try
        {
            const string payloadText =
                "MSH|^~\\&|ACME|FACILITY|RECV|RECVFAC|20260101010101||ADT^A01|MSGID-RSCKT|P|2.5\r"
                + "EVN|A01|20260101010101\r"
                + "PID|1||MRN-12345||DOE^JANE||19800101|F\r";

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            await Send_Mllp_Frame_Async(client.GetStream(), payloadText);

            var captured = await capturedTransport.WaitFor_Async(timeout: TimeSpan.FromSeconds(5));
            Assert.NotNull(captured);
            // The transport sees the inner payload bytes — framing removed.
            var receivedText = System.Text.Encoding.UTF8.GetString(captured!.Payload.Span);
            Assert.Equal(payloadText, receivedText);
            Assert.Equal(options.DefaultFlowId, captured.FlowId);
        }
        finally
        {
            await hosted.StopAsync(CancellationToken.None);
            await cts.CancelAsync();
        }
    }

    [Fact]
    public async Task Mllp_Source_Respects_Max_Connections_When_Real_Sockets_Async()
    {
        var port = FindFreePort();
        var options = new MllpInboundOptions
        {
            ListenAddress = "127.0.0.1",
            ListenPort = port,
            DefaultFlowId = Guid.NewGuid(),
            MaxConnections = 1,
        };
        var services = new ServiceCollection();
        services.AddSingleton<IInboundTransport>(new CapturingInboundTransport());
        await using var provider = services.BuildServiceProvider();

        var hosted = new MllpInboundHostedService(
            new TestOptionsMonitor<MllpInboundOptions>(options),
            new PassThroughInboundMessageFactory(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            new NoOpClockSkewMonitor(),
            new NoOpClockSkewCorrectionEventSink(),
            TimeProvider.System,
            NullLogger<MllpInboundHostedService>.Instance);

        var cts = new CancellationTokenSource();
        await hosted.StartAsync(cts.Token);
        await Task.Delay(500);

        try
        {
            using var first = new TcpClient();
            await first.ConnectAsync(IPAddress.Loopback, port);

            using var second = new TcpClient();
            await second.ConnectAsync(IPAddress.Loopback, port);

            // Second connection should be closed by the listener within a short window.
            var secondClosed = await Wait_Until_Closed_Async(second, TimeSpan.FromSeconds(3));
            Assert.True(secondClosed, "Connection past MaxConnections should be closed.");
        }
        finally
        {
            await hosted.StopAsync(CancellationToken.None);
            await cts.CancelAsync();
        }
    }

    private static async Task Send_Mllp_Frame_Async(NetworkStream stream, string payloadText)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes(payloadText);
        var framed = new byte[payload.Length + 3];
        framed[0] = StartBlock;
        Array.Copy(payload, 0, framed, 1, payload.Length);
        framed[^2] = EndBlock;
        framed[^1] = CarriageReturn;
        await stream.WriteAsync(framed.AsMemory());
        await stream.FlushAsync();
    }

    private static int FindFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static async Task<bool> Wait_Until_Closed_Async(TcpClient client, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!client.Connected)
                return true;
            try
            {
                using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                var buf = new byte[1];
                var read = await client.GetStream().ReadAsync(buf.AsMemory(), cts2.Token);
                if (read == 0)
                    return true;
            }
            catch (OperationCanceledException) { /* still alive */ }
            catch { return true; }
            await Task.Delay(50);
        }
        return false;
    }

    private sealed class CapturingInboundTransport : IInboundTransport
    {
        private readonly TaskCompletionSource<IntegrationMessage> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<InboundReceiveResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
        {
            _tcs.TrySetResult(message);
            return Task.FromResult(new InboundReceiveResult { Succeeded = true });
        }

        public async Task<IntegrationMessage?> WaitFor_Async(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                return await _tcs.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) { return null; }
        }
    }

    private sealed class PassThroughInboundMessageFactory : IInboundMessageFactory
    {
        public IntegrationMessage Create(
            Guid flowId,
            ReadOnlyMemory<byte> payload,
            PayloadFormat format,
            string? correlationId,
            IReadOnlyDictionary<string, string>? metadata,
            DateTimeOffset? receivedAtUtc = null) => new()
            {
                Id = Guid.NewGuid(),
                FlowId = flowId,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
                Payload = payload,
                PayloadFormat = format,
                ReceivedAtUtc = receivedAtUtc ?? DateTimeOffset.UtcNow,
            };
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public TestOptionsMonitor(T value) => CurrentValue = value;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class NoOpClockSkewMonitor : IClockSkewMonitor
    {
        public void Record(ClockSkewObservation observation) { }
        public IReadOnlyList<ClockSkewStatus> List() => [];
    }

    private sealed class NoOpClockSkewCorrectionEventSink : IClockSkewCorrectionEventSink
    {
        public Task PublishAsync(ClockSkewCorrectionResult result, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
