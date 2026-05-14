namespace Dialysis.BuildingBlocks.Transponder.Sagas;

/// <summary>
/// Saga logic for one inbound message contract. Register with <see cref="TransponderSagaBuilderExtensions.AddSagaMessageHandler{TState,TMessage,THandler}"/>.
/// </summary>
public interface ITransponderSagaMessageHandler<TState, TMessage>
    where TState : class, new()
    where TMessage : class
{
    /// <summary>Returns the saga partition key for this message (must be stable for the same business process).</summary>
    string GetInstanceKey(TMessage message);

    /// <summary>
    /// <paramref name="state"/> is deserialized from storage or a new instance when the saga starts.
    /// Use <paramref name="mutator"/> to persist transitions or complete the saga; publish follow-up commands with <paramref name="consumeContext"/>.Bus.
    /// </summary>
    Task HandleAsync(
        TState state,
        string currentStateName,
        TMessage message,
        ITransponderSagaMessageMutator<TState> mutator,
        ConsumeContext<TMessage> consumeContext);
}
