using BuildingBlocks;
using BuildingBlocks.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Integration event raised when any FHIR resource is written to the store.
/// </summary>
public sealed record ResourceWrittenEvent(
    Ulid CorrelationId,
    string ResourceType,
    string ResourceId,
    string VersionId,
    DateTimeOffset WrittenAt,
    IReadOnlyDictionary<string, string>? SearchContext
) : IntegrationEvent(CorrelationId);
