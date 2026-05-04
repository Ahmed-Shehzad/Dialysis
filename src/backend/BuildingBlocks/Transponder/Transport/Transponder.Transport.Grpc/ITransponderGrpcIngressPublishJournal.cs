using Grpc.Core;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Optional durability hook: invoked after validation and before in-memory fan-out so you can append to a durable store (database, append log, broker, etc.).</summary>
public interface ITransponderGrpcIngressPublishJournal
{
    /// <summary>Persist the envelope (or enqueue to an outbox). Implementations should complete before returning so a crash after return still has the write committed if your store requires that.</summary>
    ValueTask AppendAsync(TransportEnvelope envelope, ServerCallContext context, CancellationToken cancellationToken = default);
}
