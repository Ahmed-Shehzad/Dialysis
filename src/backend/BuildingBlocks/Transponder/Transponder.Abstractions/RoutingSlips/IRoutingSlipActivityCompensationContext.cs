namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// Context passed to <see cref="IRoutingSlipCompensatableActivity.CompensateAsync"/> after a later activity faults.
/// </summary>
public interface IRoutingSlipActivityCompensationContext
{
    /// <summary>Same value as the slip id returned from <see cref="ITransponderRoutingSlipStarter.StartAsync"/>.</summary>
    string TrackingNumber { get; }

    ConsumeContext<TransponderRoutingSlipContinue>? ConsumeContext { get; }

    ITransponderBus Bus { get; }

    /// <summary>The itinerary entry that had completed successfully before the fault.</summary>
    TransponderRoutingSlipCompletedActivityEntry CompletedActivity { get; }

    IReadOnlyDictionary<string, string> Variables { get; }

    void SetVariable(string key, string value);
}
