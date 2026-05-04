using Dialysis.SmartConnect;

namespace Dialysis.SmartConnect.Persistence;

public interface IMessageLedger
{
    Task AppendAsync(MessageLedgerEntry entry, CancellationToken cancellationToken);
}
