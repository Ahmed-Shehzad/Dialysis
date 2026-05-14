namespace Dialysis.BuildingBlocks.Transponder.Sagas;

/// <summary>
/// Durable snapshot of a saga instance (logical process keyed by <see cref="InstanceKey"/>).
/// </summary>
public sealed class TransponderSagaRecord
{
    /// <summary>Stable saga family id, typically <c>typeof(TState).FullName</c>.</summary>
    public required string SagaKind { get; init; }

    /// <summary>Business correlation key (order id, case id, …) shared across all messages for this saga.</summary>
    public required string InstanceKey { get; init; }

    /// <summary>Named phase for operators and logs (often an enum name).</summary>
    public required string StateName { get; init; }

    /// <summary>JSON snapshot of <typeparamref name="TState"/> when using structured saga state.</summary>
    public string? StateJson { get; init; }

    /// <summary>Optimistic concurrency token; increments on each successful commit.</summary>
    public long Version { get; init; }

    /// <summary>When true, no further messages should mutate this instance.</summary>
    public bool IsCompleted { get; init; }
}
