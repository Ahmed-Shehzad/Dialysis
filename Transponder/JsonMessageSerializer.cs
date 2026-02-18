using System.Text.Json;

using Cysharp.Serialization.Json;

using Transponder.Serialization;

using Transponder.Transports.Abstractions;

namespace Transponder;

/// <summary>
/// Serializes messages as JSON for transport payloads.
/// </summary>
public sealed class JsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonMessageSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _options.Converters.Add(new UlidJsonConverter());
        _options.Converters.Add(new EnumerationJsonConverterFactory());
    }

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Serialize(object message, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return JsonSerializer.SerializeToUtf8Bytes(message, messageType, _options);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SerializeAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        // Create a MemoryStream to hold the serialized data
        using var memoryStream = new MemoryStream();
        // Serialize the object asynchronously into the MemoryStream
        await JsonSerializer.SerializeAsync(memoryStream, message, messageType, _options, cancellationToken);

        // Return the MemoryStream content as a ReadOnlyMemory<byte>
        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public object Deserialize(ReadOnlySpan<byte> body, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return JsonSerializer.Deserialize(body, messageType, _options)
            ?? throw new InvalidOperationException("Failed to deserialize message body.");
    }

    /// <inheritdoc />
    public async Task<object?> DeserializeAsync(ReadOnlyMemory<byte> body, Type messageType, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream(body.ToArray());
        return await JsonSerializer.DeserializeAsync(memoryStream, messageType, _options, cancellationToken);
    }
}
