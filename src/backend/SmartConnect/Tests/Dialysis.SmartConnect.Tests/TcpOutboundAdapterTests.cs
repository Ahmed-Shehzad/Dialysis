using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class TcpOutboundAdapterTests
{
    private static IntegrationMessage Build(string parametersJson, byte[] payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c",
            Payload = payload,
            PayloadFormat = PayloadFormat.Binary,
            Metadata = ImmutableDictionary<string, string>.Empty
                .Add(HttpOutboundAdapter.ParametersMetadataKey, parametersJson),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

    private static (TcpListener listener, int port) StartListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return (listener, ((IPEndPoint)listener.LocalEndpoint).Port);
    }

    private async static Task<byte[]> AcceptOneAsync(TcpListener listener, int expectedBytes, CancellationToken ct)
    {
        using var client = await listener.AcceptTcpClientAsync(ct);
        await using var stream = client.GetStream();
        var buf = new byte[expectedBytes];
        var read = 0;
        while (read < expectedBytes)
        {
            var n = await stream.ReadAsync(buf.AsMemory(read), ct);
            if (n == 0) break;
            read += n;
        }

        Array.Resize(ref buf, read);
        return buf;
    }

    [Fact]
    public async Task Sends_raw_bytes_when_framing_none()
    {
        var (listener, port) = StartListener();
        try
        {
            var receivedTask = AcceptOneAsync(listener, 5, CancellationToken.None);
            using var adapter = new TcpOutboundAdapter();
            var msg = Build($$"""{"Host":"127.0.0.1","Port":{{port}},"Framing":0}""", "hello"u8.ToArray());
            var result = await adapter.SendAsync(msg, 0, CancellationToken.None);
            Assert.True(result.Succeeded, result.ErrorDetail);
            var bytes = await receivedTask;
            Assert.Equal("hello"u8.ToArray(), bytes);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task Sends_mllp_framed_payload_by_default()
    {
        var (listener, port) = StartListener();
        try
        {
            var receivedTask = AcceptOneAsync(listener, 5, CancellationToken.None);
            using var adapter = new TcpOutboundAdapter();
            var msg = Build($$"""{"Host":"127.0.0.1","Port":{{port}}}""", "AB"u8.ToArray());
            var result = await adapter.SendAsync(msg, 0, CancellationToken.None);
            Assert.True(result.Succeeded, result.ErrorDetail);
            var bytes = await receivedTask;
            Assert.Equal(new byte[] { 0x0B, (byte)'A', (byte)'B', 0x1C, 0x0D }, bytes);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task Length_prefix_uses_big_endian_int32()
    {
        var (listener, port) = StartListener();
        try
        {
            var receivedTask = AcceptOneAsync(listener, 7, CancellationToken.None);
            using var adapter = new TcpOutboundAdapter();
            var msg = Build($$"""{"Host":"127.0.0.1","Port":{{port}},"Framing":1}""", "abc"u8.ToArray());
            var result = await adapter.SendAsync(msg, 0, CancellationToken.None);
            Assert.True(result.Succeeded, result.ErrorDetail);
            var bytes = await receivedTask;
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, (byte)'a', (byte)'b', (byte)'c' }, bytes);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task Missing_parameters_returns_error()
    {
        using var adapter = new TcpOutboundAdapter();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c",
            Payload = ReadOnlyMemory<byte>.Empty,
            PayloadFormat = PayloadFormat.Binary,
            Metadata = ImmutableDictionary<string, string>.Empty,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorDetail);
    }
}
