using Transponder.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Transponder message for event export. Published to Azure Service Bus topic.
/// </summary>
public sealed class EventExportMessage : IMessage
{
    public EventExportMessage(string eventType, string payloadJson)
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        PayloadJson = payloadJson ?? throw new ArgumentNullException(nameof(payloadJson));
    }

    public string EventType { get; }
    public string PayloadJson { get; }
}
