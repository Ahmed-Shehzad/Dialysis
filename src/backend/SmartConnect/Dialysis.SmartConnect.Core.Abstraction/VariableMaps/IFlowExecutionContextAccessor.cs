namespace Dialysis.SmartConnect.VariableMaps;

/// <summary>
/// Ambient accessor for the per-dispatch <see cref="FlowExecutionContext"/>.
/// The runtime engine sets <see cref="Current"/> for the duration of each dispatch and clears it on completion.
/// Implementations must be AsyncLocal-backed so context flows through awaits within a single dispatch
/// but does not leak between concurrent dispatches.
/// </summary>
public interface IFlowExecutionContextAccessor
{
    FlowExecutionContext? Current { get; set; }
}
