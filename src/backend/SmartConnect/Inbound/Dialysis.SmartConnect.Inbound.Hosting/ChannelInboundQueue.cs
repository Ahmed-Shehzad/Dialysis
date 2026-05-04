using System.Threading.Channels;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// In-process <see cref="Channel{T}"/> backed queue for tests and single-process bridges.
/// </summary>
public sealed class ChannelInboundQueue : IInboundQueueSubscription, IDisposable
{
    private readonly Channel<InboundQueueItem> _channel = Channel.CreateUnbounded<InboundQueueItem>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public ChannelWriter<InboundQueueItem> Writer => _channel.Writer;

    public async ValueTask<InboundQueueItem?> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }

    public void Dispose() => _channel.Writer.TryComplete();
}
