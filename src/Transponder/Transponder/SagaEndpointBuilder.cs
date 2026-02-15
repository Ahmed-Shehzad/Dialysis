using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Transponder;

/// <summary>
/// Configures saga endpoints for a given saga and state type.
/// </summary>
public sealed class SagaEndpointBuilder<TSaga, TState>
    where TSaga : class
    where TState : class, ISagaState, new()
{
    private readonly SagaStyle _style;
    private readonly List<SagaMessageRegistration> _registrations = [];

    internal SagaEndpointBuilder(SagaStyle style)
    {
        _style = style;
    }

    /// <summary>
    /// Registers a message that can start a new saga instance.
    /// </summary>
    public SagaEndpointBuilder<TSaga, TState> StartWith<TMessage>(Uri inputAddress)
        where TMessage : class, IMessage
    {
        Add<TMessage>(inputAddress, startIfMissing: true);
        return this;
    }

    /// <summary>
    /// Registers a message that requires an existing saga instance.
    /// </summary>
    public SagaEndpointBuilder<TSaga, TState> Handle<TMessage>(Uri inputAddress)
        where TMessage : class, IMessage
    {
        Add<TMessage>(inputAddress, startIfMissing: false);
        return this;
    }

    internal IReadOnlyList<SagaMessageRegistration> Build() => _registrations;

    private void Add<TMessage>(Uri inputAddress, bool startIfMissing)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(inputAddress);

        var messageType = typeof(TMessage);
        var messageTypeName = messageType.FullName ?? messageType.Name;

        _registrations.Add(new SagaMessageRegistration(
            inputAddress,
            typeof(TSaga),
            typeof(TState),
            messageType,
            messageTypeName,
            startIfMissing,
            _style));
    }
}
