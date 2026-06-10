using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Dialysis.SmartConnect.Inbound.TcpListener;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class TcpListenerFrameDecoderTests
{
    [Fact]
    public void Line_Feed_Extracts_Frame_Up_To_Lf()
    {
        var data = "hello\nworld\n"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LineFeed, 4096, out var frame);

        Assert.True(found);
        Assert.Equal("hello", Encoding.UTF8.GetString(frame.ToArray()));
    }

    [Fact]
    public void Line_Feed_Returns_False_When_Incomplete()
    {
        var data = "no newline"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LineFeed, 4096, out _);

        Assert.False(found);
    }

    [Fact]
    public void Mllp_Extracts_Frame_Between_Markers()
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
    public void Length_Prefixed_Extracts_Frame()
    {
        var payload = "test data"u8.ToArray();
        var lenBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(lenBytes, (uint)payload.Length);
        var data = lenBytes.Concat(payload).ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LengthPrefixed, 4096, out var frame);

        Assert.True(found);
        Assert.Equal(payload, frame.ToArray());
    }

    [Fact]
    public void Length_Prefixed_Rejects_Oversized_Message()
    {
        var payload = "test data"u8.ToArray();
        var lenBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(lenBytes, (uint)payload.Length);
        var data = lenBytes.Concat(payload).ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        // Max is 4 bytes — smaller than payload
        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.LengthPrefixed, 4, out _);

        Assert.False(found);
    }

    [Fact]
    public void None_Returns_Entire_Buffer_Up_To_Max()
    {
        var data = "abcdefgh"u8.ToArray();
        var buffer = new ReadOnlySequence<byte>(data);

        var found = TcpListenerSourceConnector.TryReadFrame(ref buffer, FrameDecodingMode.None, 4, out var frame);

        Assert.True(found);
        Assert.Equal(4, frame.Length);
    }
}
