using Dialysis.BuildingBlocks.Transponder.Transport;

namespace Dialysis.BuildingBlocks.Transponder.Serialization;

/// <summary>
/// Serializes message contracts for <see cref="ITransponderTransport"/>.
/// </summary>
public interface IMessageSerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T message)
        where T : class;

    /// <summary>Serializes <paramref name="message"/> as an instance of <paramref name="messageType"/>.</summary>
    ReadOnlyMemory<byte> Serialize(Type messageType, object message);

    object? Deserialize(Type messageType, ReadOnlyMemory<byte> payload);
}
