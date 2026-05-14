using Dialysis.DomainDrivenDesign.DomainEvents;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.DomainDrivenDesign.DomainEvents;

/// <summary>
/// EF Core interceptor that drains <see cref="IDomainEvent"/> instances raised by tracked aggregates
/// after the database transaction commits, then dispatches each via <see cref="IDomainEventDispatcher"/>
/// in a fresh DI scope so handlers can use their own DbContext without re-entering the saving context.
/// Mirrors the existing <c>AuditSaveChangesInterceptor</c> shape so modules wire it identically.
/// </summary>
public sealed class DomainEventSaveChangesInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
    private readonly List<IDomainEvent> _pending = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchAsync(cancellationToken).ConfigureAwait(false);
        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAggregateRootMarker aggregate)
            {
                continue;
            }

            if (aggregate.DomainEvents.Count == 0)
            {
                continue;
            }

            _pending.AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }
    }

    private async Task DispatchAsync(CancellationToken cancellationToken)
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var batch = _pending.ToArray();
        _pending.Clear();

        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        foreach (var domainEvent in batch)
        {
            await dispatcher.DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
