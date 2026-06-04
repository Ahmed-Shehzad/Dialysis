using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Application payload to persist in the transactional outbox within the same database transaction as domain changes.
/// The host calls <see cref="ITransponderOutbox.EnqueueAsync"/> then saves the unit of work; a relay publishes rows where <c>ProcessedAtUtc</c> is null.
/// </summary>
public readonly record struct TransponderOutboxEnvelope
{
    /// <summary>
    /// Application payload to persist in the transactional outbox within the same database transaction as domain changes.
    /// The host calls <see cref="ITransponderOutbox.EnqueueAsync"/> then saves the unit of work; a relay publishes rows where <c>ProcessedAtUtc</c> is null.
    /// </summary>
    /// <param name="AssemblyQualifiedEventType"><see cref="Type.AssemblyQualifiedName"/> (or equivalent) used to deserialize and publish.</param>
    /// <param name="PayloadJson">JSON body for <see cref="IMessageSerializer"/>.</param>
    /// <param name="Id">Optional stable id; when null the store assigns <see cref="Guid"/>.</param>
    /// <param name="W3CTraceParent">Optional W3C traceparent for observability.</param>
    /// <param name="CorrelationId">Optional correlation forwarded to <see cref="TransponderPublishOptions"/> when the outbox relay publishes.</param>
    public TransponderOutboxEnvelope(string AssemblyQualifiedEventType,
        string PayloadJson,
        Guid? Id = null,
        string? W3CTraceParent = null,
        string? CorrelationId = null)
    {
        this.AssemblyQualifiedEventType = AssemblyQualifiedEventType;
        this.PayloadJson = PayloadJson;
        this.Id = Id;
        this.W3CTraceParent = W3CTraceParent;
        this.CorrelationId = CorrelationId;
    }

    /// <summary><see cref="Type.AssemblyQualifiedName"/> (or equivalent) used to deserialize and publish.</summary>
    public string AssemblyQualifiedEventType { get; init; }

    /// <summary>JSON body for <see cref="IMessageSerializer"/>.</summary>
    public string PayloadJson { get; init; }

    /// <summary>Optional stable id; when null the store assigns <see cref="Guid"/>.</summary>
    public Guid? Id { get; init; }

    /// <summary>Optional W3C traceparent for observability.</summary>
    public string? W3CTraceParent { get; init; }

    /// <summary>Optional correlation forwarded to <see cref="TransponderPublishOptions"/> when the outbox relay publishes.</summary>
    public string? CorrelationId { get; init; }

    public void Deconstruct(out string AssemblyQualifiedEventType, out string PayloadJson, out Guid? Id, out string? W3CTraceParent, out string? CorrelationId)
    {
        AssemblyQualifiedEventType = this.AssemblyQualifiedEventType;
        PayloadJson = this.PayloadJson;
        Id = this.Id;
        W3CTraceParent = this.W3CTraceParent;
        CorrelationId = this.CorrelationId;
    }
}
