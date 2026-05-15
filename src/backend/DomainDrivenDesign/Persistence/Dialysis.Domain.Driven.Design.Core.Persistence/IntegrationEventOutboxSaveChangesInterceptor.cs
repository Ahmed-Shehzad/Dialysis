using System.Text;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Dialysis.DomainDrivenDesign.IntegrationEvents;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// EF Core interceptor that drains <see cref="IIntegrationEvent"/> instances raised by tracked
/// aggregates and writes them as <see cref="TransponderOutboxMessageEntity"/> rows on the SAME
/// DbContext, BEFORE SaveChanges executes. Because the outbox rows go through the same transaction
/// as the aggregate writes, an aggregate is either committed alongside its integration events or
/// neither persists — the well-known transactional-outbox guarantee.
///
/// Writes outbox rows by adding entities directly via <c>context.Set&lt;TransponderOutboxMessageEntity&gt;()</c>
/// rather than going through <c>ITransponderOutbox</c>. The latter would constructor-inject the
/// DbContext, which creates a DI cycle when the interceptor itself is resolved by the DbContext
/// options factory.
///
/// A separate hosted relay (<c>TransponderOutboxRelayHostedService</c>) drains unpublished rows
/// and pushes them to the bus.
/// </summary>
public sealed class IntegrationEventOutboxSaveChangesInterceptor(
    IMessageSerializer serializer) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Enqueue(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enqueue(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Enqueue(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Collect first, then clear and add. Adding to the change tracker while iterating it
        // would invalidate the enumeration.
        List<IIntegrationEvent>? pending = null;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAggregateRootMarker aggregate)
            {
                continue;
            }

            if (aggregate.IntegrationEvents.Count == 0)
            {
                continue;
            }

            pending ??= [];
            pending.AddRange(aggregate.IntegrationEvents);
            aggregate.ClearIntegrationEvents();
        }

        if (pending is null)
        {
            return;
        }

        var outboxSet = context.Set<TransponderOutboxMessageEntity>();
        var now = DateTime.UtcNow;
        foreach (var integrationEvent in pending)
        {
            var type = integrationEvent.GetType();
            var payload = serializer.Serialize(type, integrationEvent);
            outboxSet.Add(new TransponderOutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                AssemblyQualifiedEventType = type.AssemblyQualifiedName
                    ?? type.FullName
                    ?? type.Name,
                PayloadJson = Encoding.UTF8.GetString(payload.Span),
                CreatedAtUtc = now,
            });
        }
    }
}
