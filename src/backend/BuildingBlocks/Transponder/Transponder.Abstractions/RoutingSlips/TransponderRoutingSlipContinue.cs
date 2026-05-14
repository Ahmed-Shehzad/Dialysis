namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// Internal message that advances a durable routing slip by one activity. <see cref="StepIndex"/> must match the persisted <see cref="TransponderRoutingSlipState.CurrentIndex"/> so redeliveries do not skip or repeat steps incorrectly.
/// Hosts using broker transports must subscribe to this contract (for example <c>Listen&lt;TransponderRoutingSlipContinue&gt;()</c>).
/// </summary>
public sealed class TransponderRoutingSlipContinue
{
    public required string SlipId { get; init; }

    /// <summary>0-based index of the activity to execute; must equal stored <see cref="TransponderRoutingSlipState.CurrentIndex"/>.</summary>
    public int StepIndex { get; init; }
}
