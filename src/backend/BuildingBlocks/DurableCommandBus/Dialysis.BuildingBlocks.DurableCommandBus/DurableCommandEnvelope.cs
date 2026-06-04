namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Wire format for a durable command in flight on the bus. The envelope is published once,
/// persisted by the durable transport, and consumed exactly-once (by ledger idempotency on
/// <see cref="CommandId"/>) by the per-module consumer. The handler runs against the latest
/// aggregate state; the envelope is not an event log entry and is never replayed for state
/// reconstruction.
/// </summary>
public sealed record DurableCommandEnvelope
{
    /// <summary>
    /// Wire format for a durable command in flight on the bus. The envelope is published once,
    /// persisted by the durable transport, and consumed exactly-once (by ledger idempotency on
    /// <see cref="CommandId"/>) by the per-module consumer. The handler runs against the latest
    /// aggregate state; the envelope is not an event log entry and is never replayed for state
    /// reconstruction.
    /// </summary>
    /// <param name="CommandId">Caller-supplied id; defines the idempotency key. Same id → at most one applied effect.</param>
    /// <param name="CommandTypeKey">Discriminator (assembly-qualified type name) the consumer looks up in <see cref="IDurableCommandCatalog"/>. Wire-supplied types are NOT loaded blindly; the catalog allowlist prevents arbitrary handler invocation.</param>
    /// <param name="SchemaVersion">Bump when the on-wire shape of <see cref="PayloadJson"/> changes for a given <see cref="CommandTypeKey"/>.</param>
    /// <param name="PayloadJson">JSON-serialized concrete command; deserialized against the catalog-registered CLR type.</param>
    /// <param name="CorrelationId">Stable client-visible id surfaced in the 202 response and the status endpoint URL.</param>
    /// <param name="EnqueuedAtUtc">Server-side stamp at publish time. Used for the enqueue→applied latency histogram.</param>
    /// <param name="RequestedBySubject">Authenticated subject claim at enqueue time. Surfaced on the ledger row so the status endpoint can authorize per-row reads.</param>
    public DurableCommandEnvelope(Guid CommandId,
        string CommandTypeKey,
        int SchemaVersion,
        string PayloadJson,
        string CorrelationId,
        DateTime EnqueuedAtUtc,
        string? RequestedBySubject)
    {
        this.CommandId = CommandId;
        this.CommandTypeKey = CommandTypeKey;
        this.SchemaVersion = SchemaVersion;
        this.PayloadJson = PayloadJson;
        this.CorrelationId = CorrelationId;
        this.EnqueuedAtUtc = EnqueuedAtUtc;
        this.RequestedBySubject = RequestedBySubject;
    }

    /// <summary>Caller-supplied id; defines the idempotency key. Same id → at most one applied effect.</summary>
    public Guid CommandId { get; init; }

    /// <summary>Discriminator (assembly-qualified type name) the consumer looks up in <see cref="IDurableCommandCatalog"/>. Wire-supplied types are NOT loaded blindly; the catalog allowlist prevents arbitrary handler invocation.</summary>
    public string CommandTypeKey { get; init; }

    /// <summary>Bump when the on-wire shape of <see cref="PayloadJson"/> changes for a given <see cref="CommandTypeKey"/>.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>JSON-serialized concrete command; deserialized against the catalog-registered CLR type.</summary>
    public string PayloadJson { get; init; }

    /// <summary>Stable client-visible id surfaced in the 202 response and the status endpoint URL.</summary>
    public string CorrelationId { get; init; }

    /// <summary>Server-side stamp at publish time. Used for the enqueue→applied latency histogram.</summary>
    public DateTime EnqueuedAtUtc { get; init; }

    /// <summary>Authenticated subject claim at enqueue time. Surfaced on the ledger row so the status endpoint can authorize per-row reads.</summary>
    public string? RequestedBySubject { get; init; }

    public void Deconstruct(out Guid CommandId, out string CommandTypeKey, out int SchemaVersion, out string PayloadJson, out string CorrelationId, out DateTime EnqueuedAtUtc, out string? RequestedBySubject)
    {
        CommandId = this.CommandId;
        CommandTypeKey = this.CommandTypeKey;
        SchemaVersion = this.SchemaVersion;
        PayloadJson = this.PayloadJson;
        CorrelationId = this.CorrelationId;
        EnqueuedAtUtc = this.EnqueuedAtUtc;
        RequestedBySubject = this.RequestedBySubject;
    }
}
