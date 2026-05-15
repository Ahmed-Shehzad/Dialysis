using System.Threading.Channels;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Process-local FIFO queue of export job ids awaiting background execution.
/// </summary>
public sealed class ExportJobQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public ValueTask EnqueueAsync(string jobId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);
        return _channel.Writer.WriteAsync(jobId, cancellationToken);
    }

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
