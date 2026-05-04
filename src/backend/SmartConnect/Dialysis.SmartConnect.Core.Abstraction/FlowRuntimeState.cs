namespace Dialysis.SmartConnect;

/// <summary>
/// Whether an <see cref="IntegrationFlow"/> accepts inbound traffic at runtime.
/// </summary>
public enum FlowRuntimeState
{
    Stopped = 0,
    Started = 1,
    Paused = 2,
}
