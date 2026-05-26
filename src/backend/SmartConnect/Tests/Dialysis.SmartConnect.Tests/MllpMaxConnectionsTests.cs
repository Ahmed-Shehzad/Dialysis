using System.Net;
using System.Net.Sockets;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.Mllp;
using Dialysis.SmartConnect.TimeSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers <see cref="MllpInboundOptions.MaxConnections"/> enforcement. Sockets accepted past the
/// cap are closed immediately so the listener sheds load under burst instead of queueing
/// unbounded work. Connections established within the cap continue to read normally.
/// </summary>
public sealed class MllpMaxConnectionsTests
{
    [Fact]
    public async Task Listener_Closes_Connections_Past_Max_Connections_Async()
    {
        var port = FindFreePort();
        var options = new MllpInboundOptions
        {
            ListenAddress = "127.0.0.1",
            ListenPort = port,
            DefaultFlowId = Guid.NewGuid(),
            MaxConnections = 2,
        };
        var hosted = new MllpInboundHostedService(
            new TestOptionsMonitor<MllpInboundOptions>(options),
            new StubInboundMessageFactory(),
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new NoOpClockSkewMonitor(),
            new NoOpClockSkewCorrectionEventSink(),
            TimeProvider.System,
            NullLogger<MllpInboundHostedService>.Instance);

        var cts = new CancellationTokenSource();
        var listenerTask = hosted.StartAsync(cts.Token);
        await listenerTask;
        // Give the accept loop time to bind.
        await Task.Delay(150);

        try
        {
            using var first = await Open_Client_Async(port);
            using var second = await Open_Client_Async(port);
            using var third = await Open_Client_Async(port);

            // Two connections fit; the third should be closed by the listener almost
            // immediately. Sense it via a zero-byte read after a brief delay.
            await Task.Delay(200);
            var thirdClosed = await Is_Remote_Closed_Async(third);
            var firstClosed = await Is_Remote_Closed_Async(first);
            var secondClosed = await Is_Remote_Closed_Async(second);

            Assert.True(thirdClosed, "Connection past MaxConnections should be closed by the listener.");
            Assert.False(firstClosed, "First connection should remain open under the cap.");
            Assert.False(secondClosed, "Second connection should remain open under the cap.");
        }
        finally
        {
            await hosted.StopAsync(CancellationToken.None);
            await cts.CancelAsync();
        }
    }

    private static int FindFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static async Task<TcpClient> Open_Client_Async(int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        return client;
    }

    private static async Task<bool> Is_Remote_Closed_Async(TcpClient client)
    {
        if (!client.Connected) return true;
        var buffer = new byte[1];
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var read = await client.GetStream().ReadAsync(buffer.AsMemory(), cts.Token);
            return read == 0;
        }
        catch (OperationCanceledException)
        {
            return false; // Still alive, blocking on the read.
        }
        catch
        {
            return true;
        }
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubInboundMessageFactory : IInboundMessageFactory
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

    private sealed class NoOpClockSkewMonitor : IClockSkewMonitor
    {
        public void Record(ClockSkewObservation observation)
        {
        }

        public IReadOnlyList<ClockSkewStatus> List() => Array.Empty<ClockSkewStatus>();
    }

    private sealed class NoOpClockSkewCorrectionEventSink : IClockSkewCorrectionEventSink
    {
        public Task PublishAsync(ClockSkewCorrectionResult result, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
