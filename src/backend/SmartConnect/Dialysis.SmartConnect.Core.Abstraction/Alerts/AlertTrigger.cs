namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Runtime input to the alert engine. <c>FlowRuntimeEngine</c> publishes these on every failure
/// branch (outbound send failed, transform error, unhandled exception).
/// </summary>
public sealed class AlertTrigger
{
    public Guid FlowId { get; init; }

    public Guid? MessageId { get; init; }

    public string? CorrelationId { get; init; }

    public AlertErrorType ErrorType { get; init; } = AlertErrorType.Any;

    public string? ErrorDetail { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }
}
