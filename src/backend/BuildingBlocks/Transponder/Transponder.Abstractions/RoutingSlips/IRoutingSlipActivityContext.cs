namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Execution context for a single routing slip activity.
/// </summary>
public interface IRoutingSlipActivityContext
{
    /// <summary>Slip instance id; matches <see cref="TransponderRoutingSlipActivityCompleted.TrackingNumber"/> and other routing slip event contracts.</summary>
    string SlipId { get; }

    /// <summary>Inbound delivery context when the step was triggered by <see cref="TransponderRoutingSlipContinue"/>.</summary>
    ConsumeContext<TransponderRoutingSlipContinue>? ConsumeContext { get; }

    ITransponderBus Bus { get; }

    TransponderRoutingSlipActivityRef CurrentActivity { get; }

    IReadOnlyDictionary<string, string> Variables { get; }

    void SetVariable(string key, string value);
}
