using Dialysis.SmartConnect;

namespace Dialysis.SmartConnect.Persistence;

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

    public int Skip { get; init; }

    public int Take { get; init; } = 50;
}
