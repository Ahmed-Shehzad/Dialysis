namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>Paged read access to the append-only message ledger.</summary>
public interface IMessageLedgerQuery
{
    Task<(IReadOnlyList<MessageLedgerEntry> Items, int TotalCount)> QueryAsync(
        MessageLedgerQueryCriteria criteria,
        CancellationToken cancellationToken);

    Task<MessageLedgerEntry?> GetByIdAsync(Guid ledgerEntryId, CancellationToken cancellationToken);
}

/// <summary>Filter criteria for ledger queries.</summary>
public sealed class MessageLedgerQueryCriteria
{
    public Guid? FlowId { get; init; }

    public string? CorrelationIdPrefix { get; init; }

    public DateTimeOffset? CreatedFromUtc { get; init; }

    public DateTimeOffset? CreatedToUtc { get; init; }

    /// <summary>Optional filter on ledger row status.</summary>
    public MessageLedgerStatus? Status { get; init; }

    /// <summary>Slice C2: exact-match filter on the derived <c>MessageType</c> column —
    /// e.g. <c>"ORU^R40^ORU_R40"</c>. <c>null</c> skips the predicate.</summary>
    public string? MessageType { get; init; }

    /// <summary>Slice C2: exact-match filter on the derived <c>SenderId</c> column —
    /// e.g. <c>"MachineA@FACILITY"</c>. <c>null</c> skips the predicate.</summary>
    public string? SenderId { get; init; }

    /// <summary>Slice D2: exact-match filter on the derived <c>BatchId</c> column — e.g. the
    /// absolute file path the File Reader fanned out into N messages. <c>null</c> skips the
    /// predicate.</summary>
    public string? BatchId { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; } = 50;
}
