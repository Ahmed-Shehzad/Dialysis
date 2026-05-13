namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Historical record of one alert firing — created by the engine after a rule matches and its
/// actions execute (success or failure). Persisted via <see cref="IAlertEventStore"/>.
/// </summary>
public sealed class AlertEvent
{
    public required Guid Id { get; init; }

    public required Guid RuleId { get; init; }

    public Guid? FlowId { get; init; }

    public Guid? MessageId { get; init; }

    public string? CorrelationId { get; init; }

    public AlertErrorType ErrorType { get; init; } = AlertErrorType.Any;

    public string? ErrorDetail { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public IReadOnlyList<AlertActionOutcome> ActionOutcomes { get; init; } = [];
}
