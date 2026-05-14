namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public interface IMessageLedger
{
    Task AppendAsync(MessageLedgerEntry entry, CancellationToken cancellationToken);

    Task<int> PruneAsync(DateTimeOffset olderThan, Guid? flowId = null, CancellationToken cancellationToken = default);
}
