namespace Dialysis.SmartConnect.Persistence;

public interface IMessageLedger
{
    Task AppendAsync(MessageLedgerEntry entry, CancellationToken cancellationToken);

    Task<int> PruneAsync(DateTimeOffset olderThan, Guid? flowId = null, CancellationToken cancellationToken = default);
}
