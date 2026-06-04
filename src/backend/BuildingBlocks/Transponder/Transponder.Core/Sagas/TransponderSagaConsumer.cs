using System.Text;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Sagas;

/// <summary>
/// Bridges <see cref="IConsumer{TMessage}"/> to <see cref="ITransponderSagaMessageHandler{TState,TMessage}"/> with durable state and per-instance serialization.
/// </summary>
public sealed class TransponderSagaConsumer<TState, TMessage, THandler> : IConsumer<TMessage>
    where TState : class, new()
    where TMessage : class
    where THandler : class, ITransponderSagaMessageHandler<TState, TMessage>
{
    private readonly ITransponderSagaStore _store;
    private readonly IMessageSerializer _serializer;
    private readonly THandler _handler;
    private readonly ILogger<TransponderSagaConsumer<TState, TMessage, THandler>> _logger;
    /// <summary>
    /// Bridges <see cref="IConsumer{TMessage}"/> to <see cref="ITransponderSagaMessageHandler{TState,TMessage}"/> with durable state and per-instance serialization.
    /// </summary>
    public TransponderSagaConsumer(ITransponderSagaStore store,
        IMessageSerializer serializer,
        THandler handler,
        ILogger<TransponderSagaConsumer<TState, TMessage, THandler>> logger)
    {
        _store = store;
        _serializer = serializer;
        _handler = handler;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<TMessage> context)
    {
        var sagaKind = TransponderSagaKind.For<TState>();
        var instanceKey = _handler.GetInstanceKey(context.Message);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceKey);

        var gate = TransponderSagaInstanceLock.Get(sagaKind, instanceKey);
        await gate.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            var record = await _store.GetAsync(sagaKind, instanceKey, context.CancellationToken).ConfigureAwait(false);
            if (record?.IsCompleted == true)
            {
                _logger.LogDebug("Ignoring message for completed saga {SagaKind} {InstanceKey}", sagaKind, instanceKey);
                return;
            }

            var state = DeserializeState(record?.StateJson);
            var stateName = record?.StateName ?? "Started";
            var version = record?.Version ?? 0L;

            var mutator = new TransponderSagaMutator<TState>();
            await _handler
                .HandleAsync(state, stateName, context.Message, mutator, context)
                .ConfigureAwait(false);

            if (mutator.Completed)
            {
                await _store.DeleteAsync(sagaKind, instanceKey, context.CancellationToken).ConfigureAwait(false);
                return;
            }

            if (!mutator.HasPendingUpdate || mutator.PendingState is null || mutator.PendingStateName is null)
                return;

            var json = Encoding.UTF8.GetString(_serializer.Serialize(mutator.PendingState).Span);
            var next = new TransponderSagaRecord
            {
                SagaKind = sagaKind,
                InstanceKey = instanceKey,
                StateName = mutator.PendingStateName,
                StateJson = json,
                Version = version + 1,
                IsCompleted = false,
            };

            if (version == 0L)
            {
                if (!await _store.TryInsertAsync(next, context.CancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException($"Saga insert race for {sagaKind} / {instanceKey}.");
            }
            else
            {
                if (!await _store.TryUpdateAsync(next, version, context.CancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException($"Saga concurrency conflict for {sagaKind} / {instanceKey} (expected version {version}).");
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private TState DeserializeState(string? stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
            return new TState();
        var bytes = Encoding.UTF8.GetBytes(stateJson);
        var obj = _serializer.Deserialize(typeof(TState), bytes);
        return obj as TState ?? new TState();
    }
}
