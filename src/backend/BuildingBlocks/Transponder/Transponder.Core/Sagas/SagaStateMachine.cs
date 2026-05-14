namespace Dialysis.BuildingBlocks.Transponder.Sagas;

/// <summary>
/// Declarative edges for enum-backed saga phases. Use inside <see cref="ITransponderSagaMessageHandler{TState,TMessage}.HandleAsync"/> to validate transitions.
/// </summary>
public sealed class SagaStateMachine<TState>
    where TState : struct, Enum
{
    private readonly Dictionary<(TState, Type), TState> _edges = new();

    public SagaStateMachine<TState> When<TMessage>(TState from, TState to)
        where TMessage : class
    {
        _edges[(from, typeof(TMessage))] = to;
        return this;
    }

    public bool TryGetNext(TState current, Type messageType, out TState next) =>
        _edges.TryGetValue((current, messageType), out next);

    public bool TryGetNext<TMessage>(TState current, out TState next)
        where TMessage : class =>
        TryGetNext(current, typeof(TMessage), out next);
}
