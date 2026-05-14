using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Application payload to persist in the transactional outbox within the same database transaction as domain changes.
/// The host calls <see cref="ITransponderOutbox.EnqueueAsync"/> then saves the unit of work; a relay publishes rows where <c>ProcessedAtUtc</c> is null.
/// </summary>
/// <param name="AssemblyQualifiedEventType"><see cref="Type.AssemblyQualifiedName"/> (or equivalent) used to deserialize and publish.</param>
/// <param name="PayloadJson">JSON body for <see cref="IMessageSerializer"/>.</param>
/// <param name="Id">Optional stable id; when null the store assigns <see cref="Guid"/>.</param>
/// <param name="W3CTraceParent">Optional W3C traceparent for observability.</param>
/// <param name="CorrelationId">Optional correlation forwarded to <see cref="TransponderPublishOptions"/> when the outbox relay publishes.</param>
public readonly record struct TransponderOutboxEnvelope(
    string AssemblyQualifiedEventType,
    string PayloadJson,
    Guid? Id = null,
    string? W3CTraceParent = null,
    string? CorrelationId = null);
