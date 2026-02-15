using Microsoft.EntityFrameworkCore;

namespace FhirCore.Subscriptions.Data;

public sealed class EfSubscriptionsStore : ISubscriptionsStore
{
    private readonly IDbContextFactory<SubscriptionDbContext> _factory;

    public EfSubscriptionsStore(IDbContextFactory<SubscriptionDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<SubscriptionEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Subscriptions
            .Where(s => s.Status == "active")
            .Select(s => new SubscriptionEntry
            {
                Id = s.Id,
                Criteria = s.Criteria,
                Endpoint = s.Endpoint,
                EndpointType = s.EndpointType
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var e = await db.Subscriptions.FindAsync([id], cancellationToken);
        return e is null ? null : ToEntry(e);
    }

    public async Task<SubscriptionEntry> AddAsync(SubscriptionEntry entry, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        if (await db.Subscriptions.AnyAsync(s => s.Id == entry.Id, cancellationToken))
            throw new InvalidOperationException($"Subscription '{entry.Id}' already exists.");
        var entity = new SubscriptionEntity
        {
            Id = entry.Id,
            Criteria = entry.Criteria,
            Endpoint = entry.Endpoint,
            EndpointType = entry.EndpointType,
            Status = "active"
        };
        db.Subscriptions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToEntry(entity);
    }

    public async Task<bool> UpdateAsync(string id, SubscriptionEntry entry, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var e = await db.Subscriptions.FindAsync([id], cancellationToken);
        if (e is null) return false;
        e.Criteria = entry.Criteria;
        e.Endpoint = entry.Endpoint;
        e.EndpointType = entry.EndpointType;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var e = await db.Subscriptions.FindAsync([id], cancellationToken);
        if (e is null) return false;
        db.Subscriptions.Remove(e);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SubscriptionEntry ToEntry(SubscriptionEntity e) => new()
    {
        Id = e.Id,
        Criteria = e.Criteria,
        Endpoint = e.Endpoint,
        EndpointType = e.EndpointType
    };
}
