namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

internal sealed class RoutingSlipActivityCompensationExecutionContext : IRoutingSlipActivityCompensationContext
{
    private readonly Dictionary<string, string> _variables;
    public RoutingSlipActivityCompensationExecutionContext(string trackingNumber,
        ConsumeContext<TransponderRoutingSlipContinue>? consumeContext,
        ITransponderBus bus,
        TransponderRoutingSlipCompletedActivityEntry completedActivity,
        Dictionary<string, string> variables)
    {
        _variables = variables;
        TrackingNumber = trackingNumber;
        ConsumeContext = consumeContext;
        Bus = bus;
        CompletedActivity = completedActivity;
    }
    public string TrackingNumber { get; }

    public ConsumeContext<TransponderRoutingSlipContinue>? ConsumeContext { get; }

    public ITransponderBus Bus { get; }

    public TransponderRoutingSlipCompletedActivityEntry CompletedActivity { get; }

    public IReadOnlyDictionary<string, string> Variables => _variables;

    public void SetVariable(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _variables[key] = value ?? string.Empty;
    }
}
