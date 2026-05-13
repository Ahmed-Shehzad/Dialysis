namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Plug-in surface for delivering an alert. Built-in providers: <c>email</c>, <c>webhook</c>,
/// <c>channel-redispatch</c>. Mirth UG p318 "Alert Actions".
/// </summary>
public interface IAlertActionProvider
{
    string Kind { get; }

    Task<AlertActionResult> ExecuteAsync(
        AlertEvent evt,
        AlertRule rule,
        AlertActionSlot slot,
        CancellationToken cancellationToken);
}
