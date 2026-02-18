using System.Text;

using Confluent.Kafka;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Kafka;

internal static class KafkaTransportHeaders
{
    public static Headers BuildHeaders(ITransportMessage message)
    {
        var headers = new Headers();

        foreach (KeyValuePair<string, object?> header in message.Headers)
        {
            if (header.Value is null) continue;

            headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString() ?? string.Empty));
        }

        if (!string.IsNullOrWhiteSpace(message.ContentType)) headers.Add("ContentType", Encoding.UTF8.GetBytes(message.ContentType));

        if (!string.IsNullOrWhiteSpace(message.MessageType)) headers.Add("MessageType", Encoding.UTF8.GetBytes(message.MessageType));

        if (message.CorrelationId.HasValue && !headers.Any(h => h.Key.Equals("CorrelationId", StringComparison.OrdinalIgnoreCase))) headers.Add("CorrelationId", Encoding.UTF8.GetBytes(message.CorrelationId.Value.ToString()));

        if (message.ConversationId.HasValue && !headers.Any(h => h.Key.Equals("ConversationId", StringComparison.OrdinalIgnoreCase))) headers.Add("ConversationId", Encoding.UTF8.GetBytes(message.ConversationId.Value.ToString()));

        return headers;
    }

    public static Dictionary<string, object?> ReadHeaders(Headers? headers)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (headers is null) return result;

        foreach (IHeader? header in headers)
            result[header.Key] = header.GetValueBytes() is { Length: > 0 } bytes
                ? Encoding.UTF8.GetString(bytes)
                : null;

        return result;
    }
}
