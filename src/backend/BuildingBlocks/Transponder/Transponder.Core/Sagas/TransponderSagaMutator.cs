namespace Dialysis.BuildingBlocks.Transponder;

internal sealed class TransponderSagaMutator<TState> : ITransponderSagaMessageMutator<TState>
    where TState : class, new()
{
    private bool _completed;
    private TState? _pendingState;
    private string? _pendingStateName;

    public bool Completed => _completed;

    public bool HasPendingUpdate => _pendingState is not null && !_completed;

    public TState? PendingState => _pendingState;

    public string? PendingStateName => _pendingStateName;

    public void Update(TState newState, string stateName)
    {
        ArgumentNullException.ThrowIfNull(newState);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        if (_completed)
            throw new InvalidOperationException("Cannot update a saga that is already marked complete in this turn.");
        _pendingState = newState;
        _pendingStateName = stateName;
    }

    public void Complete()
    {
        if (_pendingState is not null)
            throw new InvalidOperationException("Call Complete or Update, not both, in a single handler invocation.");
        _completed = true;
    }
}
