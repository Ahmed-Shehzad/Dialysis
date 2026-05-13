using Dialysis.SmartConnect.Alerts;

namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Default no-op <see cref="IAlertSink"/>. Registered via <c>TryAddSingleton</c> so consumers that
/// don't enable the alert engine keep working unchanged; <c>AlertEngine</c> replaces the binding
/// when the alerts feature is wired up.
/// </summary>
public sealed class NullAlertSink : IAlertSink
{
    public Task PublishAsync(AlertTrigger trigger, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
