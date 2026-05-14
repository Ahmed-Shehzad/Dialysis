namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

internal sealed class RoutingSlipActivityExecutionContext(
    string slipId,
    ConsumeContext<TransponderRoutingSlipContinue>? consumeContext,
    ITransponderBus bus,
    TransponderRoutingSlipActivityRef currentActivity,
    Dictionary<string, string> variables) : IRoutingSlipActivityContext
{
    public string SlipId { get; } = slipId;

    public ConsumeContext<TransponderRoutingSlipContinue>? ConsumeContext { get; } = consumeContext;

    public ITransponderBus Bus { get; } = bus;

    public TransponderRoutingSlipActivityRef CurrentActivity { get; } = currentActivity;

    public IReadOnlyDictionary<string, string> Variables => variables;

    public void SetVariable(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        variables[key] = value ?? string.Empty;
    }
}
