namespace Dialysis.BuildingBlocks.Transponder.Sagas;

/// <summary>Collects state transitions requested by <see cref="ITransponderSagaMessageHandler{TState,TMessage}"/> during one delivery.</summary>
public interface ITransponderSagaMessageMutator<in TState>
    where TState : class, new()
{
    /// <summary>Persist new state and advance the named phase.</summary>
    void Update(TState newState, string stateName);

    /// <summary>Marks the saga finished and removes durable state on commit.</summary>
    void Complete();
}
