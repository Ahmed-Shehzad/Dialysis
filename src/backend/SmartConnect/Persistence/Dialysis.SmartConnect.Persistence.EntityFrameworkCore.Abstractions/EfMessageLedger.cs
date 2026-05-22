using System.Collections.Immutable;
using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfMessageLedger(SmartConnectDbContext db) : IMessageLedger
{
    public async Task AppendAsync(MessageLedgerEntry entry, CancellationToken cancellationToken)
    {
        db.MessageLedgerEntries.Add(
            new MessageLedgerEntryEntity
            {
                Id = entry.Id,
                FlowId = entry.FlowId,
                IntegrationMessageId = entry.IntegrationMessageId,
                CorrelationId = entry.CorrelationId,
                Status = (int)entry.Status,
                OutboundRouteOrdinal = entry.OutboundRouteOrdinal,
                Detail = entry.Detail,
                PayloadSnapshot = entry.PayloadSnapshot,
                MetadataJson = SerializeMetadata(entry.Metadata),
                CreatedAtUtc = entry.CreatedAtUtc,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PruneAsync(DateTimeOffset olderThan, Guid? flowId = null, CancellationToken cancellationToken = default)
    {
        var query = db.MessageLedgerEntries.Where(e => e.CreatedAtUtc < olderThan);
        if (flowId is { } fid)
        {
            query = query.Where(e => e.FlowId == fid);
        }

        return await query.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Empty metadata is persisted as <c>null</c> so the existing index footprint doesn't grow
    /// for the common case (most ledger rows in tests have no operator-relevant metadata).
    /// </summary>
    internal static string? SerializeMetadata(ImmutableDictionary<string, string> metadata) =>
        metadata.IsEmpty ? null : JsonSerializer.Serialize<IReadOnlyDictionary<string, string>>(metadata);

    internal static ImmutableDictionary<string, string> DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrEmpty(metadataJson))
            return ImmutableDictionary<string, string>.Empty;
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
        return dict is null ? ImmutableDictionary<string, string>.Empty : dict.ToImmutableDictionary();
    }
}
