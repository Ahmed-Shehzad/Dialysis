namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

internal sealed class RoutingSlipActivityExecutionContext : IRoutingSlipActivityContext
{
    private readonly Dictionary<string, string> _variables;
    public RoutingSlipActivityExecutionContext(string slipId,
        ConsumeContext<TransponderRoutingSlipContinue>? consumeContext,
        ITransponderBus bus,
        TransponderRoutingSlipActivityRef currentActivity,
        Dictionary<string, string> variables)
    {
        _variables = variables;
        SlipId = slipId;
        ConsumeContext = consumeContext;
        Bus = bus;
        CurrentActivity = currentActivity;
    }
    public string SlipId { get; }

    public ConsumeContext<TransponderRoutingSlipContinue>? ConsumeContext { get; }

    public ITransponderBus Bus { get; }

    public TransponderRoutingSlipActivityRef CurrentActivity { get; }

    public IReadOnlyDictionary<string, string> Variables => _variables;

    public void SetVariable(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _variables[key] = value ?? string.Empty;
    }
}
