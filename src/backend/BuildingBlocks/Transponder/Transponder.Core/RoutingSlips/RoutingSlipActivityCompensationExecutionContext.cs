namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

internal sealed class RoutingSlipActivityCompensationExecutionContext(
    string trackingNumber,
    ConsumeContext<TransponderRoutingSlipContinue>? consumeContext,
    ITransponderBus bus,
    TransponderRoutingSlipCompletedActivityEntry completedActivity,
    Dictionary<string, string> variables) : IRoutingSlipActivityCompensationContext
{
    public string TrackingNumber { get; } = trackingNumber;

    public ConsumeContext<TransponderRoutingSlipContinue>? ConsumeContext { get; } = consumeContext;

    public ITransponderBus Bus { get; } = bus;

    public TransponderRoutingSlipCompletedActivityEntry CompletedActivity { get; } = completedActivity;

    public IReadOnlyDictionary<string, string> Variables => variables;

    public void SetVariable(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        variables[key] = value ?? string.Empty;
    }
}
