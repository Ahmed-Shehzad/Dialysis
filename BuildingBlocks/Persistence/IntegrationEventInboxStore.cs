using BuildingBlocks.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence;

/// <summary>
/// Inbox store implementation using EF Core. Requires DbContext to have DbSet&lt;IntegrationEventInboxEntity&gt;
/// and apply <see cref="IntegrationEventInboxConfiguration"/>.
/// </summary>
public sealed class IntegrationEventInboxStore : IIntegrationEventInboxStore
{
    private readonly DbContext _context;

    public IntegrationEventInboxStore(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await _context.Set<IntegrationEventInboxEntity>()
            .AnyAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(string messageId, string? eventType, string? tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var entity = new IntegrationEventInboxEntity
        {
            MessageId = messageId,
            EventType = eventType,
            TenantId = tenantId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        };
        _ = await _context.Set<IntegrationEventInboxEntity>().AddAsync(entity, cancellationToken);
    }
}
