using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder;

internal sealed class ConsumeRouteContributor<TMessage> : IConsumeRouteContributor
    where TMessage : class
{
    public void Contribute(Dictionary<string, TransponderConsumeRouteEntry> routes)
    {
        var key = RoutingKey.For<TMessage>();
        if (routes.ContainsKey(key))
            return;

        routes[key] = new TransponderConsumeRouteEntry(
            Deserialize: static (serializer, payload) => serializer.Deserialize(typeof(TMessage), payload) as TMessage,
            InvokeConsumers: static async (provider, messageObj, bus, correlationId, deduplicationId, cancellationToken) =>
            {
                var typed = (TMessage)messageObj;
                var consumers = provider.GetServices<IConsumer<TMessage>>().ToArray();
                foreach (var consumer in consumers)
                    await consumer.HandleAsync(new ConsumeContext<TMessage>(typed, cancellationToken, bus, correlationId, deduplicationId))
                        .ConfigureAwait(false);
            });
    }
}
