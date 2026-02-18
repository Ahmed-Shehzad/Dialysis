using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder;

internal sealed class SagaReceiveEndpointHandler
{
    private static readonly MethodInfo InvokeMethod =
        typeof(SagaReceiveEndpointHandler).GetMethod(
            nameof(InvokeInternalAsync),
            BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException("Saga handler invoker method not found.");

    private readonly Uri _inputAddress;
    private readonly SagaEndpointRegistry _registry;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SagaReceiveEndpointHandler> _logger;

    public SagaReceiveEndpointHandler(
        Uri inputAddress,
        SagaEndpointRegistry registry,
        IMessageSerializer serializer,
        IServiceScopeFactory scopeFactory,
        ILogger<SagaReceiveEndpointHandler> logger)
    {
        _inputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(IReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ITransportMessage transportMessage = context.Message;
        string? messageTypeName = transportMessage.MessageType;

        if (string.IsNullOrWhiteSpace(messageTypeName))
        {
            _logger.LogWarning(
                "SagaReceiveEndpointHandler missing message type. InputAddress={InputAddress}",
                _inputAddress);
            return;
        }

        if (!_registry.TryGetHandlers(_inputAddress, messageTypeName, out IReadOnlyList<SagaMessageRegistration> registrations))
        {
            _logger.LogDebug(
                "SagaReceiveEndpointHandler no handlers found. InputAddress={InputAddress}, MessageType={MessageType}",
                _inputAddress,
                messageTypeName);
            return;
        }

        _logger.LogDebug(
            "SagaReceiveEndpointHandler dispatching handlers. InputAddress={InputAddress}, MessageType={MessageType}, HandlerCount={HandlerCount}",
            _inputAddress,
            messageTypeName,
            registrations.Count);

        using IServiceScope scope = _scopeFactory.CreateScope();

        Type messageType = registrations[0].MessageType;
        object message = _serializer.Deserialize(transportMessage.Body.Span, messageType);

        foreach (SagaMessageRegistration registration in registrations)
        {
            var task = (Task)InvokeMethod.MakeGenericMethod(
                    registration.SagaType,
                    registration.StateType,
                    registration.MessageType)
                .Invoke(
                    null,
                    [
                        scope.ServiceProvider,
                        registration,
                        message,
                        transportMessage,
                        context.SourceAddress,
                        context.DestinationAddress,
                        context.CancellationToken
                    ])!;

            await task.ConfigureAwait(false);
        }
    }

    public async static Task InvokeInternalAsync<TSaga, TState, TMessage>(
        IServiceProvider serviceProvider,
        SagaMessageRegistration registration,
        object message,
        ITransportMessage transportMessage,
        Uri? sourceAddress,
        Uri? destinationAddress,
        CancellationToken cancellationToken)
        where TSaga : class
        where TState : class, ISagaState, new()
        where TMessage : class, IMessage
    {
        var typedMessage = (TMessage)message;
        TransponderBus bus = serviceProvider.GetRequiredService<TransponderBus>();
        var consumeContext = new ConsumeContext<TMessage>(
            typedMessage,
            transportMessage,
            sourceAddress,
            destinationAddress,
            cancellationToken,
            bus);

        TransponderMessageContext messageContext = TransponderMessageContextFactory.FromTransportMessage(
            transportMessage,
            sourceAddress,
            destinationAddress);
        using IDisposable? scope = bus.BeginConsumeScope(messageContext);

        Ulid? correlationId = consumeContext.CorrelationId ?? consumeContext.ConversationId;
        if (!TryEnsureCorrelationId(serviceProvider, correlationId, typeof(TMessage).Name))
            return;

        ISagaRepository<TState> repository = serviceProvider.GetRequiredService<ISagaRepository<TState>>();
        (TState? state, bool isNew) = await GetOrCreateStateAsync(
            registration,
            repository,
            consumeContext,
            correlationId!.Value,
            cancellationToken).ConfigureAwait(false);
        if (state is null)
            return;

        TSaga saga = serviceProvider.GetRequiredService<TSaga>();
        if (saga is not ISagaMessageHandler<TState, TMessage> handler)
            throw new InvalidOperationException(
                $"{typeof(TSaga).Name} does not implement ISagaMessageHandler<{typeof(TState).Name}, {typeof(TMessage).Name}>.");

        var sagaContext = new SagaConsumeContext<TState, TMessage>(
            consumeContext,
            state,
            registration.Style,
            isNew);

        bool skipHandler = await ShouldSkipHandlerAfterStepsAsync(saga, sagaContext, cancellationToken).ConfigureAwait(false);
        if (!skipHandler)
            await handler.HandleAsync(sagaContext).ConfigureAwait(false);

        await PersistSagaStateAsync(serviceProvider, repository, state, sagaContext.IsCompleted, typeof(TMessage).Name, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryEnsureCorrelationId(IServiceProvider serviceProvider, Ulid? correlationId, string messageTypeName)
    {
        if (correlationId.HasValue)
            return true;

        ILogger<SagaReceiveEndpointHandler>? logger = serviceProvider.GetService<ILogger<SagaReceiveEndpointHandler>>();
        logger?.LogWarning(
            "SagaReceiveEndpointHandler missing correlation id. MessageType={MessageType}",
            messageTypeName);
        return false;
    }

    private async static Task<(TState? state, bool isNew)> GetOrCreateStateAsync<TState, TMessage>(SagaMessageRegistration registration,
        ISagaRepository<TState> repository,
        ConsumeContext<TMessage> consumeContext,
        Ulid correlationId,
        CancellationToken cancellationToken)
        where TState : class, ISagaState, new()
        where TMessage : class, IMessage
    {
        TState? state = await repository.GetAsync(correlationId, cancellationToken).ConfigureAwait(false);

        if (state is null)
        {
            if (!registration.StartIfMissing)
                return (null, false);

            state = new TState
            {
                CorrelationId = correlationId,
                ConversationId = consumeContext.ConversationId
            };
            return (state, true);
        }

        if (state.CorrelationId == Ulid.Empty)
            state.CorrelationId = correlationId;
        if (state.ConversationId == null && consumeContext.ConversationId.HasValue)
            state.ConversationId = consumeContext.ConversationId;

        return (state, false);
    }

    private async static Task<bool> ShouldSkipHandlerAfterStepsAsync<TState, TMessage>(
        object saga,
        SagaConsumeContext<TState, TMessage> sagaContext,
        CancellationToken cancellationToken)
        where TState : class, ISagaState
        where TMessage : class, IMessage
    {
        if (saga is not ISagaStepProvider<TState, TMessage> stepProvider)
            return false;

        IEnumerable<SagaStep<TState>> steps = stepProvider.GetSteps(sagaContext) ?? [];
        SagaStatus status = await sagaContext.ExecuteStepsAsync(steps, cancellationToken).ConfigureAwait(false);
        return status != SagaStatus.Completed;
    }

    private async static Task PersistSagaStateAsync<TState>(
        IServiceProvider serviceProvider,
        ISagaRepository<TState> repository,
        TState state,
        bool isCompleted,
        string messageTypeName,
        CancellationToken cancellationToken)
        where TState : class, ISagaState
    {
        if (isCompleted)
        {
            await repository.DeleteAsync(state.CorrelationId, cancellationToken).ConfigureAwait(false);
            return;
        }

        bool saved = await repository.SaveAsync(state, cancellationToken).ConfigureAwait(false);
        if (saved)
            return;

        ILogger<SagaReceiveEndpointHandler>? logger = serviceProvider.GetService<ILogger<SagaReceiveEndpointHandler>>();
        logger?.LogWarning(
            "SagaReceiveEndpointHandler: Concurrency conflict saving saga state. CorrelationId={CorrelationId}, MessageType={MessageType}, Version={Version}",
            state.CorrelationId,
            messageTypeName,
            state.Version);
    }
}
