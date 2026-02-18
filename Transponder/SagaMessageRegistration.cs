using Transponder.Abstractions;

namespace Transponder;

internal sealed class SagaMessageRegistration
{
    public SagaMessageRegistration(
        Uri inputAddress,
        Type sagaType,
        Type stateType,
        Type messageType,
        string messageTypeName,
        bool startIfMissing,
        SagaStyle style)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        SagaType = sagaType ?? throw new ArgumentNullException(nameof(sagaType));
        StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        MessageTypeName = messageTypeName ?? throw new ArgumentNullException(nameof(messageTypeName));
        StartIfMissing = startIfMissing;
        Style = style;
    }

    public Uri InputAddress { get; }

    public Type SagaType { get; }

    public Type StateType { get; }

    public Type MessageType { get; }

    public string MessageTypeName { get; }

    public bool StartIfMissing { get; }

    public SagaStyle Style { get; }
}
