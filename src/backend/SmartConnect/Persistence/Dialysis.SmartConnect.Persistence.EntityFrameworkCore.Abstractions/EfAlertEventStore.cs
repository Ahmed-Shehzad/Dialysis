using System.Text.Json;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfAlertEventStore(SmartConnectDbContext db) : IAlertEventStore
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = null };

    public async Task AppendAsync(AlertEvent evt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        db.AlertEvents.Add(new AlertEventEntity
        {
            Id = evt.Id == Guid.Empty ? Guid.CreateVersion7() : evt.Id,
            RuleId = evt.RuleId,
            FlowId = evt.FlowId,
            MessageId = evt.MessageId,
            CorrelationId = evt.CorrelationId,
            ErrorType = (int)evt.ErrorType,
            ErrorDetail = Truncate(evt.ErrorDetail, 2000),
            OccurredAtUtc = evt.OccurredAtUtc == default ? DateTimeOffset.UtcNow : evt.OccurredAtUtc,
            ActionOutcomesJson = JsonSerializer.Serialize(evt.ActionOutcomes, _jsonOpts),
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AlertEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await db.AlertEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<AlertEvent>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var rows = await db.AlertEvents.AsNoTracking()
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<AlertEvent>> GetForRuleAsync(Guid ruleId, int take, CancellationToken cancellationToken = default)
    {
        var rows = await db.AlertEvents.AsNoTracking()
            .Where(e => e.RuleId == ruleId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.AlertEvents.Where(e => e.OccurredAtUtc < cutoffUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0) return 0;
        db.AlertEvents.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    private static AlertEvent ToDomain(AlertEventEntity entity)
    {
        var outcomes = JsonSerializer.Deserialize<List<AlertActionOutcome>>(entity.ActionOutcomesJson, _jsonOpts) ?? [];
        return new AlertEvent
        {
            Id = entity.Id,
            RuleId = entity.RuleId,
            FlowId = entity.FlowId,
            MessageId = entity.MessageId,
            CorrelationId = entity.CorrelationId,
            ErrorType = (AlertErrorType)entity.ErrorType,
            ErrorDetail = entity.ErrorDetail,
            OccurredAtUtc = entity.OccurredAtUtc,
            ActionOutcomes = outcomes,
        };
    }

    private static string? Truncate(string? s, int max) =>
        s is null ? null : s.Length <= max ? s : s[..max];
}
