namespace Dialysis.SmartConnect.Inbound.Mllp;

/// <summary>
/// Incremental MLLP frame decoder: start block 0x0B, end sequence 0x1C 0x0D (payload excludes framing bytes).
/// </summary>
public sealed class MllpFrameDecoder
{
    private const byte StartBlock = 0x0B;
    private const byte EndBlock = 0x1C;
    private const byte CarriageReturn = 0x0D;

    private readonly int _maxMessageBytes;
    private readonly List<byte> _buffer = new(4096);
    private readonly Queue<byte[]> _completed = new();
    private DecoderState _state = DecoderState.SeekStart;

    private enum DecoderState
    {
        SeekStart,
        InMessage,
        ExpectCr,
    }

    public MllpFrameDecoder(int maxMessageBytes) => _maxMessageBytes = maxMessageBytes;

    /// <summary>Appends received bytes; completed frames are queued for <see cref="TryTakeMessage"/>.</summary>
    public void Append(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            ProcessByte(b);
        }
    }

    private void ProcessByte(byte b)
    {
        switch (_state)
        {
            case DecoderState.SeekStart:
                OnSeekStart(b);
                break;
            case DecoderState.InMessage:
                OnInMessage(b);
                break;
            case DecoderState.ExpectCr:
                OnExpectCr(b);
                break;
        }
    }

    private void OnSeekStart(byte b)
    {
        if (b == StartBlock)
        {
            _state = DecoderState.InMessage;
            _buffer.Clear();
        }
    }

    private void OnInMessage(byte b)
    {
        if (b == StartBlock)
        {
            _buffer.Clear();
            return;
        }

        if (b == EndBlock)
        {
            _state = DecoderState.ExpectCr;
            return;
        }

        if (_buffer.Count >= _maxMessageBytes)
        {
            ResetInternal();
            return;
        }

        _buffer.Add(b);
    }

    private void OnExpectCr(byte b)
    {
        if (b == CarriageReturn)
        {
            _completed.Enqueue(_buffer.ToArray());
            _buffer.Clear();
            _state = DecoderState.SeekStart;
            return;
        }

        ResetInternal();
        if (b == StartBlock)
        {
            _state = DecoderState.InMessage;
            _buffer.Clear();
        }
    }

    /// <summary>Returns true when at least one full frame was decoded.</summary>
    public bool TryTakeMessage(out byte[]? payload)
    {
        if (_completed.Count == 0)
        {
            payload = null;
            return false;
        }

        payload = _completed.Dequeue();
        return true;
    }

    /// <summary>Clears decoder state and queued frames.</summary>
    public void Reset()
    {
        ResetInternal();
        _completed.Clear();
    }

    private void ResetInternal()
    {
        _state = DecoderState.SeekStart;
        _buffer.Clear();
    }
}
