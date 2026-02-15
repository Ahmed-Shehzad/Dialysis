using Google.Protobuf;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc;

internal static class GrpcTransportMessageMapper
{
    public static GrpcTransportMessage ToProto(ITransportMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var proto = new GrpcTransportMessage
        {
            Body = ByteString.CopyFrom(message.Body.ToArray()),
            ContentType = message.ContentType ?? string.Empty,
            MessageId = message.MessageId?.ToString() ?? string.Empty,
            CorrelationId = message.CorrelationId?.ToString() ?? string.Empty,
            ConversationId = message.ConversationId?.ToString() ?? string.Empty,
            MessageType = message.MessageType ?? string.Empty,
            SentTimeUnixMs = message.SentTime?.ToUnixTimeMilliseconds() ?? 0
        };

        foreach (var header in message.Headers)
        {
            if (header.Value is null) continue;

            proto.Headers[header.Key] = header.Value.ToString() ?? string.Empty;
        }

        return proto;
    }

    public static ITransportMessage FromProto(GrpcTransportMessage message)
    {
        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> header in message.Headers) headers[header.Key] = header.Value;

        DateTimeOffset? sentTime = null;
        if (message.SentTimeUnixMs > 0) sentTime = DateTimeOffset.FromUnixTimeMilliseconds(message.SentTimeUnixMs);

        return new TransportMessage(
            message.Body.ToByteArray(),
            string.IsNullOrWhiteSpace(message.ContentType) ? null : message.ContentType,
            headers,
            Ulid.TryParse(message.MessageId, out var messageId) ? messageId : null,
            Ulid.TryParse(message.CorrelationId, out var correlationId) ? correlationId : null,
            Ulid.TryParse(message.ConversationId, out var conversationId) ? conversationId : null,
            string.IsNullOrWhiteSpace(message.MessageType) ? null : message.MessageType,
            sentTime);
    }
}
