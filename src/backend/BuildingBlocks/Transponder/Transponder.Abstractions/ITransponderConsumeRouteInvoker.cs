namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Dispatches inbound payloads to registered message routes (built from <c>AddConsumer&lt;T,...&gt;</c> and transport <c>Listen&lt;T&gt;</c>). Routing keys match <see cref="Type.FullName"/> (or <see cref="Type.Name"/> when full name is null).
/// </summary>
public interface ITransponderConsumeRouteInvoker
{
    bool HasRoute(string routingKey);

    object? Deserialize(string routingKey, ReadOnlyMemory<byte> payload, IMessageSerializer serializer);

    Task InvokeConsumersAsync(
        IServiceProvider provider,
        string routingKey,
        object message,
        ITransponderBus bus,
        string? correlationId,
        string? deduplicationId,
        CancellationToken cancellationToken);
}
