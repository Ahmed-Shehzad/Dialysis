namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Enqueues integration payloads into the transactional outbox. Implementations attach rows to the current EF Core context; the application must call <c>SaveChanges</c> (or the ambient unit of work) in the same transaction as business data.
/// </summary>
public interface ITransponderOutbox
{
    /// <summary>
    /// Adds an outbox row. Does not call <c>SaveChanges</c>.
    /// </summary>
    Task EnqueueAsync(TransponderOutboxEnvelope envelope, CancellationToken cancellationToken = default);
}
