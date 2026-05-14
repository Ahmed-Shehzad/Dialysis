using Dialysis.BuildingBlocks.Transponder.Sagas;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Durable row for <see cref="ITransponderSagaStore"/> (multi-node safe with optimistic concurrency on <see cref="Version"/>).
/// </summary>
public sealed class TransponderSagaInstanceEntity
{
    public Guid Id { get; set; }

    public string SagaKind { get; set; } = string.Empty;

    public string InstanceKey { get; set; } = string.Empty;

    public string StateName { get; set; } = string.Empty;

    public string? StateJson { get; set; }

    /// <summary>Increments on each successful transition; used as an EF concurrency token.</summary>
    public long Version { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
