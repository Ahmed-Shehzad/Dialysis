using System.Text;
using Dialysis.SmartConnect.Inbound.Mllp;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class MllpFrameDecoderTests
{
    [Fact]
    public void Try_Take_Message_Single_Chunk_Yields_Payload_Without_Framing()
    {
        var dec = new MllpFrameDecoder(1024);
        var inner = "MSH|test"u8.ToArray();
        var frame = new byte[1 + inner.Length + 2];
        frame[0] = 0x0B;
        inner.AsSpan().CopyTo(frame.AsSpan(1));
        frame[^2] = 0x1C;
        frame[^1] = 0x0D;

        dec.Append(frame);
        Assert.True(dec.TryTakeMessage(out var payload));
        Assert.NotNull(payload);
        Assert.Equal(inner, payload);
        Assert.False(dec.TryTakeMessage(out _));
    }

    [Fact]
    public void Try_Take_Message_Split_Chunks_Yields_One_Message()
    {
        var dec = new MllpFrameDecoder(1024);
        var msg = "ACK"u8.ToArray();
        var full = new List<byte> { 0x0B };
        full.AddRange(msg);
        full.Add(0x1C);
        full.Add(0x0D);

        var arr = full.ToArray();
        dec.Append(arr.AsSpan(0, 2));
        dec.Append(arr.AsSpan(2, 2));
        dec.Append(arr.AsSpan(4, arr.Length - 4));

        Assert.True(dec.TryTakeMessage(out var payload));
        Assert.Equal(msg, payload);
    }

    [Fact]
    public void Try_Take_Message_Two_Frames_Queues_Two()
    {
        var dec = new MllpFrameDecoder(1024);
        void Frame(string s)
        {
            var b = Encoding.UTF8.GetBytes(s);
            var f = new byte[1 + b.Length + 2];
            f[0] = 0x0B;
            b.AsSpan().CopyTo(f.AsSpan(1));
            f[^2] = 0x1C;
            f[^1] = 0x0D;
            dec.Append(f);
        }

        Frame("A");
        Frame("B");

        Assert.True(dec.TryTakeMessage(out var p1));
        Assert.Equal("A"u8.ToArray(), p1);
        Assert.True(dec.TryTakeMessage(out var p2));
        Assert.Equal("B"u8.ToArray(), p2);
    }

    [Fact]
    public void Append_Exceeds_Max_Discards_And_Recovers()
    {
        const int max = 2;
        var dec = new MllpFrameDecoder(max);
        // Start + 3 payload bytes (over Max) before end — should reset on third add
        dec.Append([0x0B, 0x41, 0x42, 0x43, 0x1C, 0x0D]);
        Assert.False(dec.TryTakeMessage(out _));

        // Valid small frame after bad one
        dec.Append([0x0B, 0x7A, 0x1C, 0x0D]);
        Assert.True(dec.TryTakeMessage(out var p));
        Assert.Equal(new byte[] { 0x7A }, p);
    }
}
