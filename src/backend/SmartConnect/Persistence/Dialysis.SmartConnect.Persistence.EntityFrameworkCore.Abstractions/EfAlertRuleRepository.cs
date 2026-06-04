using System.Text.Json;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core <see cref="IAlertRuleRepository"/>. Each rule's <c>EnabledFlowIds</c>, error patterns and
/// action slots live as JSON columns — low cardinality and admin-only, so no relational decomposition.
/// </summary>
public sealed class EfAlertRuleRepository : IAlertRuleRepository
{
    private readonly SmartConnectDbContext _db;
    /// <summary>
    /// EF Core <see cref="IAlertRuleRepository"/>. Each rule's <c>EnabledFlowIds</c>, error patterns and
    /// action slots live as JSON columns — low cardinality and admin-only, so no relational decomposition.
    /// </summary>
    public EfAlertRuleRepository(SmartConnectDbContext db) => _db = db;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = null };

    public async Task<IReadOnlyList<AlertRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.AlertRules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(ToDomain)];
    }

    public async Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.AlertRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<AlertRule>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.AlertRules.AsNoTracking()
            .Where(r => r.Enabled)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(ToDomain)];
    }

    public async Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var existing = await _db.AlertRules.FirstOrDefaultAsync(r => r.Id == rule.Id, cancellationToken).ConfigureAwait(false);
        var enabledFlowIdsJson = JsonSerializer.Serialize(rule.EnabledFlowIds ?? (IReadOnlyList<Guid>)[], _jsonOpts);
        var patternsJson = JsonSerializer.Serialize(rule.ErrorPatterns, _jsonOpts);
        var actionsJson = JsonSerializer.Serialize(rule.Actions, _jsonOpts);
        var throttleSeconds = (int)(rule.ThrottleWindow?.TotalSeconds ?? 0);
        var modifiedUtc = rule.LastModifiedUtc == default ? DateTimeOffset.UtcNow : rule.LastModifiedUtc;

        if (existing is null)
        {
            _db.AlertRules.Add(new AlertRuleEntity
            {
                Id = rule.Id,
                Name = rule.Name,
                Enabled = rule.Enabled,
                Description = rule.Description,
                EnabledFlowIdsJson = enabledFlowIdsJson,
                ErrorPatternsJson = patternsJson,
                ActionsJson = actionsJson,
                ThrottleWindowSeconds = throttleSeconds,
                Revision = rule.Revision,
                LastModifiedUtc = modifiedUtc,
            });
        }
        else
        {
            existing.Name = rule.Name;
            existing.Enabled = rule.Enabled;
            existing.Description = rule.Description;
            existing.EnabledFlowIdsJson = enabledFlowIdsJson;
            existing.ErrorPatternsJson = patternsJson;
            existing.ActionsJson = actionsJson;
            existing.ThrottleWindowSeconds = throttleSeconds;
            existing.Revision = rule.Revision;
            existing.LastModifiedUtc = modifiedUtc;
        }
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.AlertRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return;
        _db.AlertRules.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static AlertRule ToDomain(AlertRuleEntity entity)
    {
        var enabledFlowIds = JsonSerializer.Deserialize<List<Guid>>(entity.EnabledFlowIdsJson, _jsonOpts) ?? [];
        var patterns = JsonSerializer.Deserialize<List<AlertErrorPattern>>(entity.ErrorPatternsJson, _jsonOpts) ?? [];
        var actions = JsonSerializer.Deserialize<List<AlertActionSlot>>(entity.ActionsJson, _jsonOpts) ?? [];

        return new AlertRule
        {
            Id = entity.Id,
            Name = entity.Name,
            Enabled = entity.Enabled,
            Description = entity.Description,
            EnabledFlowIds = enabledFlowIds.Count == 0 ? null : enabledFlowIds,
            ErrorPatterns = patterns,
            Actions = actions,
            ThrottleWindow = entity.ThrottleWindowSeconds > 0
                ? TimeSpan.FromSeconds(entity.ThrottleWindowSeconds)
                : null,
            Revision = entity.Revision,
            LastModifiedUtc = entity.LastModifiedUtc,
        };
    }
}
