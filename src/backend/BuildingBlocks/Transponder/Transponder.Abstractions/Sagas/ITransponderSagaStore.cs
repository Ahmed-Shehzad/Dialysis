namespace Dialysis.BuildingBlocks.Transponder.Sagas;

/// <summary>
/// Persists saga instances for long-running processes. Implement with EF Core, Redis, or other storage; Transponder.Core ships an in-memory store for development and tests.
/// </summary>
public interface ITransponderSagaStore
{
    Task<TransponderSagaRecord?> GetAsync(string sagaKind, string instanceKey, CancellationToken cancellationToken = default);

    /// <summary>Creates a row only when none exists for the pair (<paramref name="sagaKind"/>, <paramref name="instanceKey"/>).</summary>
    Task<bool> TryInsertAsync(TransponderSagaRecord record, CancellationToken cancellationToken = default);

    /// <summary>Replaces the row when <see cref="TransponderSagaRecord.Version"/> matches the stored version (then bumps version by one in storage).</summary>
    Task<bool> TryUpdateAsync(TransponderSagaRecord record, long expectedVersion, CancellationToken cancellationToken = default);

    Task DeleteAsync(string sagaKind, string instanceKey, CancellationToken cancellationToken = default);
}
