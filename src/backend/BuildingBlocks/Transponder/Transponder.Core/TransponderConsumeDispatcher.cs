using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Invokes registered <see cref="IConsumer{TMessage}"/> instances after a message is deserialized from a transport.
/// </summary>
public sealed class TransponderConsumeDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITransponderConsumeRouteInvoker _routes;
    /// <summary>
    /// Invokes registered <see cref="IConsumer{TMessage}"/> instances after a message is deserialized from a transport.
    /// </summary>
    public TransponderConsumeDispatcher(IServiceScopeFactory scopeFactory, ITransponderConsumeRouteInvoker routes)
    {
        _scopeFactory = scopeFactory;
        _routes = routes;
    }
    public Task DispatchAsync(
        string routingKey,
        ReadOnlyMemory<byte> payload,
        IMessageSerializer serializer,
        ITransponderBus bus,
        CancellationToken cancellationToken) =>
        DispatchAsync(routingKey, payload, correlationId: null, deduplicationId: null, serializer, bus, cancellationToken);

    public Task DispatchAsync(
        string routingKey,
        ReadOnlyMemory<byte> payload,
        string? correlationId,
        IMessageSerializer serializer,
        ITransponderBus bus,
        CancellationToken cancellationToken) =>
        DispatchAsync(routingKey, payload, correlationId, deduplicationId: null, serializer, bus, cancellationToken);

    public async Task DispatchAsync(
        string routingKey,
        ReadOnlyMemory<byte> payload,
        string? correlationId,
        string? deduplicationId,
        IMessageSerializer serializer,
        ITransponderBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(routingKey);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(bus);

        if (!_routes.HasRoute(routingKey))
            return;

        var deserialized = _routes.Deserialize(routingKey, payload, serializer);
        if (deserialized is null)
            return;

        var useInbox = !string.IsNullOrWhiteSpace(deduplicationId);
        if (useInbox)
        {
            await using var inboxScope = _scopeFactory.CreateAsyncScope();
            if (inboxScope.ServiceProvider.GetService<ITransponderInboxGate>() is { } acquireGate
                && !await acquireGate.TryAcquireAsync(deduplicationId!, routingKey, cancellationToken).ConfigureAwait(false))
                return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            await _routes
                .InvokeConsumersAsync(provider, routingKey, deserialized, bus, correlationId, deduplicationId, cancellationToken)
                .ConfigureAwait(false);

            if (useInbox)
            {
                await using var completeScope = _scopeFactory.CreateAsyncScope();
                if (completeScope.ServiceProvider.GetService<ITransponderInboxGate>() is { } completeGate)
                    await completeGate.CompleteAsync(deduplicationId!, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            if (useInbox)
            {
                await using var abandonScope = _scopeFactory.CreateAsyncScope();
                if (abandonScope.ServiceProvider.GetService<ITransponderInboxGate>() is { } abandonGate)
                    await abandonGate.AbandonAsync(deduplicationId!, cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
    }
}
