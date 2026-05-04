using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.Messaging;

namespace Dialysis.HIS.PatientFlow.Integration;

internal static class OutboxFlush
{
    public static async Task ForAggregateAsync<TId>(
        AggregateRoot<TId> aggregate,
        ITransponderOutbox outbox,
        CancellationToken cancellationToken)
        where TId : notnull
    {
        foreach (var evt in aggregate.IntegrationEvents.ToArray())
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(evt), cancellationToken).ConfigureAwait(false);
        aggregate.ClearIntegrationEvents();
    }
}
