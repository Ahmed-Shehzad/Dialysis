namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Receives <see cref="AlertTrigger"/>s from <c>FlowRuntimeEngine</c>. Default implementation is a no-op
/// (<c>NullAlertSink</c>) so existing tests that don't wire the alert engine keep working; the alert
/// engine replaces the registration when the feature is enabled.
/// </summary>
public interface IAlertSink
{
    Task PublishAsync(AlertTrigger trigger, CancellationToken cancellationToken);
}
