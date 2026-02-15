using System.Threading.Channels;

namespace Transponder.Transports.SSE;

/// <summary>
/// Represents an active SSE client connection.
/// </summary>
public sealed class SseClientConnection
{
    private readonly Channel<SseEvent> _channel;

    public SseClientConnection(
        string id,
        string? userId,
        IReadOnlyList<string> streams,
        IReadOnlyList<string> groups,
        int bufferCapacity)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        ArgumentNullException.ThrowIfNull(streams);
        ArgumentNullException.ThrowIfNull(groups);
        Streams = streams;
        Groups = groups;

        var options = new BoundedChannelOptions(bufferCapacity <= 0 ? 128 : bufferCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite,
            AllowSynchronousContinuations = false
        };

        _channel = Channel.CreateBounded<SseEvent>(options);
    }

    public string Id { get; }

    public string? UserId { get; }

    public IReadOnlyList<string> Streams { get; }

    public IReadOnlyList<string> Groups { get; }

    internal bool TryEnqueue(SseEvent message) => _channel.Writer.TryWrite(message);

    internal ChannelReader<SseEvent> Reader => _channel.Reader;

    internal void Complete() => _channel.Writer.TryComplete();
}
