using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Invokes registered <see cref="IConsumer{TMessage}"/> instances after a message is deserialized from a transport.
/// </summary>
public sealed class TransponderConsumeDispatcher(IServiceScopeFactory scopeFactory, ITransponderConsumeRouteInvoker routes)
{
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

        if (!routes.HasRoute(routingKey))
            return;

        var deserialized = routes.Deserialize(routingKey, payload, serializer);
        if (deserialized is null)
            return;

        var useInbox = !string.IsNullOrWhiteSpace(deduplicationId);
        if (useInbox)
        {
            await using var inboxScope = scopeFactory.CreateAsyncScope();
            if (inboxScope.ServiceProvider.GetService<ITransponderInboxGate>() is { } acquireGate
                && !await acquireGate.TryAcquireAsync(deduplicationId!, routingKey, cancellationToken).ConfigureAwait(false))
                return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            await routes
                .InvokeConsumersAsync(provider, routingKey, deserialized, bus, correlationId, deduplicationId, cancellationToken)
                .ConfigureAwait(false);

            if (useInbox)
            {
                await using var completeScope = scopeFactory.CreateAsyncScope();
                if (completeScope.ServiceProvider.GetService<ITransponderInboxGate>() is { } completeGate)
                    await completeGate.CompleteAsync(deduplicationId!, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            if (useInbox)
            {
                await using var abandonScope = scopeFactory.CreateAsyncScope();
                if (abandonScope.ServiceProvider.GetService<ITransponderInboxGate>() is { } abandonGate)
                    await abandonGate.AbandonAsync(deduplicationId!, cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
    }
}
