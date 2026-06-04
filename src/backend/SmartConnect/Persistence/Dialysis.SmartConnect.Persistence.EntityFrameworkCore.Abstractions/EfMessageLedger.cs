using System.Collections.Immutable;
using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfMessageLedger : IMessageLedger
{
    private readonly SmartConnectDbContext _db;
    public EfMessageLedger(SmartConnectDbContext db) => _db = db;
    public async Task AppendAsync(MessageLedgerEntry entry, CancellationToken cancellationToken)
    {
        var (messageType, senderId, batchId) = DeriveSearchableColumns(entry.Metadata);
        _db.MessageLedgerEntries.Add(
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
                MessageType = messageType,
                SenderId = senderId,
                BatchId = batchId,
                CreatedAtUtc = entry.CreatedAtUtc,
            });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PruneAsync(DateTimeOffset olderThan, Guid? flowId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.MessageLedgerEntries.Where(e => e.CreatedAtUtc < olderThan);
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

    /// <summary>
    /// Slice C2 + D2: extract the dashboard-filterable columns from the incoming metadata
    /// bag. Top-level <c>LedgerSearchKeys</c> / <c>BatchMetadataKeys</c> win; when absent for
    /// MessageType / SenderId, fall back to the legacy <c>smartconnect.sourcemap.json</c>
    /// blob the MLLP listener used to be the only producer of. BatchId has no legacy fallback
    /// — it ships via <see cref="BatchMetadataKeys.BatchId"/> only.
    /// </summary>
    internal static (string? MessageType, string? SenderId, string? BatchId) DeriveSearchableColumns(
        ImmutableDictionary<string, string> metadata)
    {
        if (metadata.IsEmpty)
            return (null, null, null);

        metadata.TryGetValue(LedgerSearchKeys.MessageType, out var messageType);
        metadata.TryGetValue(LedgerSearchKeys.SenderId, out var senderId);
        metadata.TryGetValue(BatchMetadataKeys.BatchId, out var batchId);

        if (!string.IsNullOrWhiteSpace(messageType) && !string.IsNullOrWhiteSpace(senderId))
        {
            return (
                messageType,
                senderId,
                string.IsNullOrWhiteSpace(batchId) ? null : Truncate(batchId, 1024));
        }

        // Fall back to the legacy sourcemap.json — pre-C2 inbound transports populated only
        // this blob. Parsing failures swallow silently (best-effort projection).
        if (metadata.TryGetValue("smartconnect.sourcemap.json", out var blob) &&
            !string.IsNullOrWhiteSpace(blob))
        {
            try
            {
                using var doc = JsonDocument.Parse(blob);
                if (string.IsNullOrWhiteSpace(messageType) &&
                    doc.RootElement.TryGetProperty("hl7.messageType", out var mt) &&
                    mt.ValueKind == JsonValueKind.String)
                {
                    messageType = mt.GetString();
                }

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    var app = doc.RootElement.TryGetProperty("hl7.sendingApplication", out var a) && a.ValueKind == JsonValueKind.String
                        ? a.GetString()
                        : null;
                    var facility = doc.RootElement.TryGetProperty("hl7.sendingFacility", out var f) && f.ValueKind == JsonValueKind.String
                        ? f.GetString()
                        : null;
                    senderId = (app, facility) switch
                    {
                        ({ Length: > 0 }, { Length: > 0 }) => $"{app}@{facility}",
                        ({ Length: > 0 }, _) => app,
                        _ => null,
                    };
                }
            }
            catch (JsonException)
            {
                // Malformed blob — leave the derived columns null.
            }
        }

        return (
            string.IsNullOrWhiteSpace(messageType) ? null : Truncate(messageType, 256),
            string.IsNullOrWhiteSpace(senderId) ? null : Truncate(senderId, 256),
            string.IsNullOrWhiteSpace(batchId) ? null : Truncate(batchId, 1024));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
