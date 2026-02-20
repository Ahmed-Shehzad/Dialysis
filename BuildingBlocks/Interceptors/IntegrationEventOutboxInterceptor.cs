using BuildingBlocks.Abstractions;
using BuildingBlocks.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using System.Text.Json;

namespace BuildingBlocks.Interceptors;

/// <summary>
/// EF Core interceptor that persists integration events to the Outbox table in the same transaction
/// as business data. Runs in <c>SavingChangesAsync</c> before the DB write — ensures atomicity.
/// A background publisher reads the Outbox and publishes to Transponder.
/// </summary>
public sealed class IntegrationEventOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IIntegrationEventBuffer _buffer;

    public IntegrationEventOutboxInterceptor(IIntegrationEventBuffer buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await PersistToOutboxAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private Task PersistToOutboxAsync(DbContext context, CancellationToken _)
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

        IReadOnlyList<IIntegrationEvent> bufferedEvents = _buffer.Drain();
        var allEvents = integrationEvents.Concat(bufferedEvents).ToList();

        if (allEvents.Count == 0)
            return Task.CompletedTask;

        var outboxRows = new List<IntegrationEventOutboxEntity>();
        foreach (IIntegrationEvent evt in allEvents)
        {
            string eventType = evt.GetType().AssemblyQualifiedName ?? evt.GetType().FullName!;
            string payload = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions);
            outboxRows.Add(new IntegrationEventOutboxEntity
            {
                Id = Ulid.NewUlid(),
                EventType = eventType,
                Payload = payload,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        context.Set<IntegrationEventOutboxEntity>().AddRange(outboxRows);
        return Task.CompletedTask;
    }
}
