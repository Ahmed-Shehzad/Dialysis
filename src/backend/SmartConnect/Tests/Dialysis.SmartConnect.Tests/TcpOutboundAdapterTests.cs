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

    private static (TcpListener listener, int port) Start_Listener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return (listener, ((IPEndPoint)listener.LocalEndpoint).Port);
    }

    private static async Task<byte[]> Acceptone_Async(TcpListener listener, int expectedBytes, CancellationToken ct)
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
    public async Task Sends_Raw_Bytes_When_Framing_None_Async()
    {
        var (listener, port) = Start_Listener();
        try
        {
            var receivedTask = Acceptone_Async(listener, 5, CancellationToken.None);
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
    public async Task Sends_Mllp_Framed_Payload_By_Default_Async()
    {
        var (listener, port) = Start_Listener();
        try
        {
            var receivedTask = Acceptone_Async(listener, 5, CancellationToken.None);
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
    public async Task Length_Prefix_Uses_Big_Endian_Int32_Async()
    {
        var (listener, port) = Start_Listener();
        try
        {
            var receivedTask = Acceptone_Async(listener, 7, CancellationToken.None);
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
    public async Task Missing_Parameters_Returns_Error_Async()
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
