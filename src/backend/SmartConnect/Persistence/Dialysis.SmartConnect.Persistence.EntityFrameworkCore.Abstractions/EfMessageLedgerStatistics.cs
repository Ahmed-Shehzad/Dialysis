using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfMessageLedgerStatistics(SmartConnectDbContext db) : IMessageLedgerStatistics
{
    public async Task<IReadOnlyList<FlowStatusCount>> GetFlowStatisticsAsync(
        Guid flowId,
        CancellationToken cancellationToken)
    {
        var results = await db.MessageLedgerEntries
            .AsNoTracking()
            .Where(e => e.FlowId == flowId)
            .GroupBy(e => e.Status)
            .Select(g => new FlowStatusCount
            {
                Status = (MessageLedgerStatus)g.Key,
                Count = g.LongCount(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results;
    }
}
