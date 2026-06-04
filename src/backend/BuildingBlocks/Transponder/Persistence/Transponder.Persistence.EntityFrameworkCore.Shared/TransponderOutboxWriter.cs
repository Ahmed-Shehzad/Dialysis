namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Adds outbox rows to the supplied <typeparamref name="TContext"/>; the host must save the same context in the ambient transaction.
/// </summary>
public sealed class TransponderOutboxWriter<TContext> : ITransponderOutbox
    where TContext : TransponderPersistenceDbContextBase
{
    private readonly TContext _db;
    /// <summary>
    /// Adds outbox rows to the supplied <typeparamref name="TContext"/>; the host must save the same context in the ambient transaction.
    /// </summary>
    public TransponderOutboxWriter(TContext db) => _db = db;
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
        _db.OutboxMessages.Add(row);
        return Task.CompletedTask;
    }
}
