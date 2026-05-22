using System.Globalization;

namespace Dialysis.SmartConnect;

/// <summary>
/// Slice D of the SmartConnect ↔ Mirth alignment plan: a batch is a group of related
/// integration messages sourced from one inbound event — a single file containing N
/// HL7v2 messages, a database query that returned N rows, a delimited-text file split
/// into N records, etc. Mirth Connect uses dedicated message-metadata columns for batch
/// id / sequence; we layer the same shape on top of slice C's <c>MetadataJson</c>
/// column so no schema churn is required. The operator dashboard's existing metadata
/// filter then trivially supports "show me every message in batch X".
/// </summary>
public readonly record struct BatchContext(
    string BatchId,
    int Sequence,
    int Total,
    string? Source)
{
    /// <summary>True when this row is the final message in its batch.</summary>
    public bool IsLast => Sequence == Total;

    /// <summary>True when this row is the first message in its batch.</summary>
    public bool IsFirst => Sequence == 1;
}

/// <summary>
/// Stable string keys for batch context on the <see cref="IntegrationMessage.Metadata"/>
/// dictionary. Held in one place so inbound transports, transforms, and the operator
/// shell all agree on the lookup string.
/// </summary>
public static class BatchMetadataKeys
{
    public const string BatchId = "smartconnect.batch.id";
    public const string Sequence = "smartconnect.batch.sequence";
    public const string Total = "smartconnect.batch.total";

    /// <summary>Optional free-form provenance — e.g. <c>file:lab-results-2026-05-22.txt</c>
    /// or <c>query:select-pending-orders</c>. Surfaced on the dashboard so operators can
    /// trace a misbehaving batch back to its inbound source.</summary>
    public const string Source = "smartconnect.batch.source";
}

/// <summary>
/// Ergonomic accessors over the metadata bag. Kept as extension methods (not properties
/// on <see cref="IntegrationMessage"/>) so the message type stays a thin DTO.
/// </summary>
public static class IntegrationMessageBatchExtensions
{
    /// <summary>
    /// Returns a copy of the message tagged with batch context. Inbound transports call
    /// this on each emitted message; iterator transforms call it when fanning a parent
    /// payload into N child messages.
    /// </summary>
    public static IntegrationMessage WithBatch(
        this IntegrationMessage message,
        string batchId,
        int sequence,
        int total,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Batch sequence is 1-based.");
        if (total < 1)
            throw new ArgumentOutOfRangeException(nameof(total), total, "Batch total must be positive.");
        if (sequence > total)
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Sequence cannot exceed total.");

        var tagged = message
            .WithMetadata(BatchMetadataKeys.BatchId, batchId)
            .WithMetadata(BatchMetadataKeys.Sequence, sequence.ToString(CultureInfo.InvariantCulture))
            .WithMetadata(BatchMetadataKeys.Total, total.ToString(CultureInfo.InvariantCulture));
        return string.IsNullOrWhiteSpace(source) ? tagged : tagged.WithMetadata(BatchMetadataKeys.Source, source);
    }

    /// <summary>
    /// Reads the four batch keys off the metadata bag into a typed
    /// <see cref="BatchContext"/>. Returns <c>false</c> when any required key is missing
    /// or unparseable so callers can fall through to the "no batch context" path.
    /// </summary>
    public static bool TryGetBatch(this IntegrationMessage message, out BatchContext context)
    {
        ArgumentNullException.ThrowIfNull(message);

        context = default;
        if (!message.Metadata.TryGetValue(BatchMetadataKeys.BatchId, out var batchId) || string.IsNullOrWhiteSpace(batchId))
            return false;
        if (!message.Metadata.TryGetValue(BatchMetadataKeys.Sequence, out var seqRaw) ||
            !int.TryParse(seqRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seq))
            return false;
        if (!message.Metadata.TryGetValue(BatchMetadataKeys.Total, out var totalRaw) ||
            !int.TryParse(totalRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total))
            return false;
        message.Metadata.TryGetValue(BatchMetadataKeys.Source, out var source);
        context = new BatchContext(batchId, seq, total, source);
        return true;
    }
}
