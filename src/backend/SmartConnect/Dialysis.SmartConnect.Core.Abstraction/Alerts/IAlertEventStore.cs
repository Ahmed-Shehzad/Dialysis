namespace Dialysis.SmartConnect.Alerts;

public interface IAlertEventStore
{
    Task AppendAsync(AlertEvent evt, CancellationToken cancellationToken = default);

    Task<AlertEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertEvent>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertEvent>> GetForRuleAsync(Guid ruleId, int take, CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default);
}
