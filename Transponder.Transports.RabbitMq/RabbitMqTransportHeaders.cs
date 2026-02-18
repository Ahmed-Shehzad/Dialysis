using System.Text;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.RabbitMq;

internal static class RabbitMqTransportHeaders
{
    public static IDictionary<string, object?> BuildHeaders(ITransportMessage message)
    {
        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object?> header in message.Headers)
        {
            if (header.Value is null) continue;

            headers[header.Key] = Encoding.UTF8.GetBytes(header.Value.ToString() ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(message.ContentType)) headers["ContentType"] = Encoding.UTF8.GetBytes(message.ContentType);

        if (!string.IsNullOrWhiteSpace(message.MessageType)) headers["MessageType"] = Encoding.UTF8.GetBytes(message.MessageType);

        if (message.CorrelationId.HasValue && !headers.ContainsKey("CorrelationId")) headers["CorrelationId"] = Encoding.UTF8.GetBytes(message.CorrelationId.Value.ToString());

        if (message.ConversationId.HasValue && !headers.ContainsKey("ConversationId")) headers["ConversationId"] = Encoding.UTF8.GetBytes(message.ConversationId.Value.ToString());

        return headers;
    }

    public static Dictionary<string, object?> ReadHeaders(IDictionary<string, object?>? headers)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (headers is null) return result;

        foreach (KeyValuePair<string, object?> header in headers)
            result[header.Key] = header.Value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                _ => header.Value?.ToString()
            };

        return result;
    }
}
