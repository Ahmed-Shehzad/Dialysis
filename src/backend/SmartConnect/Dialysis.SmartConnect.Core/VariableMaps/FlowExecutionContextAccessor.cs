namespace Dialysis.SmartConnect.VariableMaps;

/// <summary>
/// AsyncLocal-backed <see cref="IFlowExecutionContextAccessor"/> (same pattern as <c>IHttpContextAccessor</c>).
/// </summary>
public sealed class FlowExecutionContextAccessor : IFlowExecutionContextAccessor
{
    private static readonly AsyncLocal<FlowExecutionContext?> _current = new();

    public FlowExecutionContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
