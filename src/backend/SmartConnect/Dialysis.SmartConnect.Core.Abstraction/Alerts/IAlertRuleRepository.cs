namespace Dialysis.SmartConnect.Alerts;

public interface IAlertRuleRepository
{
    Task<IReadOnlyList<AlertRule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertRule>> GetEnabledAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
