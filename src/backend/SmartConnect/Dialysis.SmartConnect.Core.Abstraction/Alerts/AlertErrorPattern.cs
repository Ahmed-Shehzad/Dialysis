namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// One match pattern inside an <see cref="AlertRule"/>. A rule fires when at least one of its
/// patterns matches the incoming <see cref="AlertTrigger"/>. Mirth UG p316 "Alert Error Types and Regex".
/// </summary>
public sealed class AlertErrorPattern
{
    public AlertErrorType ErrorType { get; init; } = AlertErrorType.Any;

    /// <summary>Optional regex matched against <see cref="AlertTrigger.ErrorDetail"/>. Null = match all.</summary>
    public string? Regex { get; init; }
}
