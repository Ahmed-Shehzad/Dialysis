using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Transponder;

/// <summary>
/// Default saga consume context implementation.
/// </summary>
public sealed class SagaConsumeContext<TState, TMessage> : ISagaConsumeContext<TState, TMessage>
    where TState : class, ISagaState
    where TMessage : class, IMessage
{
    private readonly IConsumeContext<TMessage> _consumeContext;

    public SagaConsumeContext(
        IConsumeContext<TMessage> consumeContext,
        TState saga,
        SagaStyle style,
        bool isNew)
    {
        _consumeContext = consumeContext ?? throw new ArgumentNullException(nameof(consumeContext));
        Saga = saga ?? throw new ArgumentNullException(nameof(saga));
        Style = style;
        IsNew = isNew;
    }

    public TState Saga { get; }

    public bool IsNew { get; }

    public SagaStyle Style { get; }

    public bool IsCompleted { get; private set; }

    public Task<SagaStatus> ExecuteStepsAsync(
        IEnumerable<SagaStep<TState>> steps,
        CancellationToken cancellationToken = default)
        => SagaExecution.ExecuteAsync(Style, Saga, steps, cancellationToken);

    public void MarkCompleted() => IsCompleted = true;

    public Ulid? MessageId => _consumeContext.MessageId;

    public Ulid? CorrelationId => _consumeContext.CorrelationId;

    public Ulid? ConversationId => _consumeContext.ConversationId;

    public Uri? SourceAddress => _consumeContext.SourceAddress;

    public Uri? DestinationAddress => _consumeContext.DestinationAddress;

    public DateTimeOffset? SentTime => _consumeContext.SentTime;

    public IReadOnlyDictionary<string, object?> Headers => _consumeContext.Headers;

    public CancellationToken CancellationToken => _consumeContext.CancellationToken;

    public Task PublishAsync<TMessageOut>(TMessageOut message, CancellationToken cancellationToken = default)
        where TMessageOut : class, IMessage
        => _consumeContext.PublishAsync(message, cancellationToken);

    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        => _consumeContext.GetSendEndpointAsync(address, cancellationToken);

    public Task RespondAsync<TResponse>(TResponse response, CancellationToken cancellationToken = default)
        where TResponse : class, IMessage
        => _consumeContext.RespondAsync(response, cancellationToken);
    public TMessage Message => _consumeContext.Message;
}
