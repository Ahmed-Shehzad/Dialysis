using BuildingBlocks.Abstractions;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Transponder.Abstractions;

namespace BuildingBlocks.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches integration events raised by <see cref="AggregateRoot"/> entities
/// after changes are persisted. Events are collected and dispatched in <see cref="SavedChangesAsync"/>
/// so that integration event handlers run outside the transactional boundary — ensuring eventual consistency.
/// Publishes to Transponder (service bus) and in-process handlers via <see cref="IPublisher"/>.
/// </summary>
public sealed class IntegrationEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPublisher _publisher;

    public IntegrationEventDispatcherInterceptor(IPublishEndpoint publishEndpoint, IPublisher publisher)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchIntegrationEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchIntegrationEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var aggregateRoots = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.IntegrationEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var integrationEvents = aggregateRoots
            .SelectMany(aggregate => aggregate.IntegrationEvents)
            .ToList();

        foreach (AggregateRoot aggregate in aggregateRoots)
            aggregate.ClearIntegrationEvents();

        foreach (IIntegrationEvent integrationEvent in integrationEvents)
        {
            await _publishEndpoint.PublishAsync(integrationEvent, cancellationToken);
            await _publisher.PublishAsync(integrationEvent, cancellationToken);
        }
    }
}
