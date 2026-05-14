using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder;

internal sealed class TransponderConsumeRouteInvoker(IEnumerable<IConsumeRouteContributor> contributors) : ITransponderConsumeRouteInvoker
{
    private readonly Dictionary<string, TransponderConsumeRouteEntry> _routes = Build(contributors);

    private static Dictionary<string, TransponderConsumeRouteEntry> Build(IEnumerable<IConsumeRouteContributor> contributors)
    {
        var routes = new Dictionary<string, TransponderConsumeRouteEntry>(StringComparer.Ordinal);
        foreach (var contributor in contributors)
            contributor.Contribute(routes);
        return routes;
    }

    public bool HasRoute(string routingKey) =>
        !string.IsNullOrEmpty(routingKey) && _routes.ContainsKey(routingKey);

    public object? Deserialize(string routingKey, ReadOnlyMemory<byte> payload, IMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        if (!_routes.TryGetValue(routingKey, out var entry))
            return null;
        return entry.Deserialize(serializer, payload);
    }

    public Task InvokeConsumersAsync(
        IServiceProvider provider,
        string routingKey,
        object message,
        ITransponderBus bus,
        string? correlationId,
        string? deduplicationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(bus);
        if (!_routes.TryGetValue(routingKey, out var entry))
            throw new InvalidOperationException($"No consume route for routing key '{routingKey}'.");

        return entry.InvokeConsumers(provider, message, bus, correlationId, deduplicationId, cancellationToken);
    }
}
