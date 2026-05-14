namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Adds outbox rows to the supplied <typeparamref name="TContext"/>; the host must save the same context in the ambient transaction.
/// </summary>
public sealed class TransponderOutboxWriter<TContext>(TContext db) : ITransponderOutbox
    where TContext : TransponderPersistenceDbContextBase
{
    public Task EnqueueAsync(TransponderOutboxEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var row = new TransponderOutboxMessageEntity
        {
            Id = envelope.Id ?? Guid.NewGuid(),
            AssemblyQualifiedEventType = envelope.AssemblyQualifiedEventType,
            PayloadJson = envelope.PayloadJson,
            CreatedAtUtc = DateTime.UtcNow,
            W3CTraceParent = envelope.W3CTraceParent,
            CorrelationId = envelope.CorrelationId,
        };
        db.OutboxMessages.Add(row);
        return Task.CompletedTask;
    }
}
