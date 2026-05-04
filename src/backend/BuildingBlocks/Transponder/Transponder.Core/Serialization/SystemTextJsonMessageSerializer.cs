using System.Text.Json;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// JSON serializer using <see cref="JsonSerializer"/> with camel-case names.
/// </summary>
public sealed class SystemTextJsonMessageSerializer(JsonSerializerOptions? options = null) : IMessageSerializer
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public ReadOnlyMemory<byte> Serialize<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return JsonSerializer.SerializeToUtf8Bytes(message, _options);
    }

    public ReadOnlyMemory<byte> Serialize(Type messageType, object message)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(message);
        if (!messageType.IsInstanceOfType(message))
            throw new ArgumentException($"Message is not an instance of {messageType}.", nameof(message));
        return JsonSerializer.SerializeToUtf8Bytes(message, messageType, _options);
    }

    public object? Deserialize(Type messageType, ReadOnlyMemory<byte> payload) =>
        JsonSerializer.Deserialize(payload.Span, messageType, _options);
}
