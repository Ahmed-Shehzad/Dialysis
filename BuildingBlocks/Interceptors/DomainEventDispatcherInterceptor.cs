using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches domain events raised by <see cref="AggregateRoot"/> entities
/// before changes are persisted. Events are collected and dispatched in <see cref="SavingChangesAsync"/>
/// so that handlers run within the same unit of work (e.g., they can modify tracked entities or enqueue
/// integration events before the transaction commits). This follows the "dispatch before save" pattern
/// where domain event handlers are part of the same transactional boundary.
/// </summary>
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcherInterceptor(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var aggregateRoots = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        foreach (AggregateRoot aggregate in aggregateRoots) aggregate.ClearDomainEvents();

        IEnumerable<Task> dispatchTasks = domainEvents.Select(domainEvent => _publisher.PublishAsync(domainEvent, cancellationToken));

        await Task.WhenAll(dispatchTasks);
    }
}
