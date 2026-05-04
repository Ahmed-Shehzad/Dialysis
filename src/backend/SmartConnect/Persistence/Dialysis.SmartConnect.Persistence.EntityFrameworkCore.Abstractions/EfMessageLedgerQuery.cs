using Dialysis.SmartConnect;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfMessageLedgerQuery(SmartConnectDbContext db) : IMessageLedgerQuery
{
    public async Task<(IReadOnlyList<MessageLedgerEntry> Items, int TotalCount)> QueryAsync(
        MessageLedgerQueryCriteria criteria,
        CancellationToken cancellationToken)
    {
        var q = db.MessageLedgerEntries.AsNoTracking().AsQueryable();
        if (criteria.FlowId is { } flowId)
        {
            q = q.Where(e => e.FlowId == flowId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CorrelationIdPrefix))
        {
            var p = criteria.CorrelationIdPrefix.Trim();
            q = q.Where(e => e.CorrelationId.StartsWith(p));
        }

        if (criteria.CreatedFromUtc is { } from)
        {
            q = q.Where(e => e.CreatedAtUtc >= from);
        }

        if (criteria.CreatedToUtc is { } to)
        {
            q = q.Where(e => e.CreatedAtUtc <= to);
        }

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var take = Math.Clamp(criteria.Take, 1, 500);
        var skip = Math.Max(0, criteria.Skip);
        var rows = await q
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(Map).ToList();
        return (items, total);
    }

    private static MessageLedgerEntry Map(MessageLedgerEntryEntity e) =>
        new()
        {
            Id = e.Id,
            FlowId = e.FlowId,
            IntegrationMessageId = e.IntegrationMessageId,
            CorrelationId = e.CorrelationId,
            Status = (MessageLedgerStatus)e.Status,
            OutboundRouteOrdinal = e.OutboundRouteOrdinal,
            Detail = e.Detail,
            PayloadSnapshot = e.PayloadSnapshot,
            CreatedAtUtc = e.CreatedAtUtc,
        };
}
