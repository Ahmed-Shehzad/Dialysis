using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Default in-process bus: resolves scoped consumers and invokes them sequentially within a scope per publish.
/// </summary>
public sealed class TransponderBus(
    IServiceScopeFactory scopeFactory,
    ITransponderConsumeRouteInvoker routes,
    IMessageSerializer serializer) : ITransponderBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class =>
        PublishAsync(message, default, cancellationToken);

    public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class =>
        PublishPreparedAsync(RoutingKey.For<TMessage>(), message, options, cancellationToken);

    public async Task PublishPreparedAsync(
        string routingKey,
        object message,
        TransponderPublishOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!routes.HasRoute(routingKey))
        {
            throw new InvalidOperationException(
                $"No consume route for routing key '{routingKey}'. Register the contract with AddConsumer<T> or your transport's Listen<T>().");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        await routes
            .InvokeConsumersAsync(provider, routingKey, message, this, options.CorrelationId, deduplicationId: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
        where TMessage : class =>
        TransponderLargeMessagePublisher.PublishAsync(this, serializer, message, options, cancellationToken);
}
