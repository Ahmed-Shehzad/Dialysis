using Dialysis.SmartConnect;
using Dialysis.SmartConnect.Persistence;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfMessageLedger(SmartConnectDbContext db) : IMessageLedger
{
    public async Task AppendAsync(MessageLedgerEntry entry, CancellationToken cancellationToken)
    {
        db.MessageLedgerEntries.Add(
            new MessageLedgerEntryEntity
            {
                Id = entry.Id,
                FlowId = entry.FlowId,
                IntegrationMessageId = entry.IntegrationMessageId,
                CorrelationId = entry.CorrelationId,
                Status = (int)entry.Status,
                OutboundRouteOrdinal = entry.OutboundRouteOrdinal,
                Detail = entry.Detail,
                PayloadSnapshot = entry.PayloadSnapshot,
                CreatedAtUtc = entry.CreatedAtUtc,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
