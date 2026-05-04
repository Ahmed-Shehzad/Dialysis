namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Canonical transactional outbox row in the client-configured schema (see migrations in the provider assembly).
/// </summary>
public sealed class TransponderOutboxMessageEntity
{
    public Guid Id { get; set; }

    public string AssemblyQualifiedEventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// W3C <c>traceparent</c> (or trace id fallback) captured at enqueue; when null, relay workers may fall back to the integration event stable id or another app correlation.
    /// </summary>
    public string? W3CTraceParent { get; set; }

    /// <summary>Optional correlation forwarded to <see cref="TransponderPublishOptions"/> when the outbox relay publishes.</summary>
    public string? CorrelationId { get; set; }
}
