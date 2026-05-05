using System.Buffers;
using Dialysis.SmartConnect.Inbound.TcpListener;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class TcpListenerFrameDecoderTests
{
    [Fact]
    public void LineFeed_extracts_frame_up_to_LF()
    {
        var data = "hello\nworld\n"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LineFeed, 4096, out var frame);

        Assert.True(found);
        Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(frame.ToArray()));
    }

    [Fact]
    public void LineFeed_returns_false_when_incomplete()
    {
        var data = "no newline"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LineFeed, 4096, out _);

        Assert.False(found);
    }

    [Fact]
    public void Mllp_extracts_frame_between_markers()
    {
        // 0x0B + payload + 0x1C + 0x0D
        var payload = "MSH|^~\\&|"u8.ToArray();
        var data = new byte[] { 0x0B }
            .Concat(payload)
            .Concat(new byte[] { 0x1C, 0x0D })
            .ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.Mllp, 4096, out var frame);

        Assert.True(found);
        Assert.Equal(payload, frame.ToArray());
    }

    [Fact]
    public void LengthPrefixed_extracts_frame()
    {
        var payload = "test data"u8.ToArray();
        var lenBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(lenBytes, (uint)payload.Length);
        var data = lenBytes.Concat(payload).ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LengthPrefixed, 4096, out var frame);

        Assert.True(found);
        Assert.Equal(payload, frame.ToArray());
    }

    [Fact]
    public void LengthPrefixed_rejects_oversized_message()
    {
        var payload = "test data"u8.ToArray();
        var lenBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(lenBytes, (uint)payload.Length);
        var data = lenBytes.Concat(payload).ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        // Max is 4 bytes — smaller than payload
        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LengthPrefixed, 4, out _);

        Assert.False(found);
    }

    [Fact]
    public void None_returns_entire_buffer_up_to_max()
    {
        var data = "abcdefgh"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.None, 4, out var frame);

        Assert.True(found);
        Assert.Equal(4, frame.Length);
    }
}
