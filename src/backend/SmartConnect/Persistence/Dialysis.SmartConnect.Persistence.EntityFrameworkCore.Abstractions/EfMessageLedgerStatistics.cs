using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfMessageLedgerStatistics : IMessageLedgerStatistics
{
    private readonly SmartConnectDbContext _db;
    public EfMessageLedgerStatistics(SmartConnectDbContext db) => _db = db;
    public async Task<IReadOnlyList<FlowStatusCount>> GetFlowStatisticsAsync(
        Guid flowId,
        CancellationToken cancellationToken)
    {
        var results = await _db.MessageLedgerEntries
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
