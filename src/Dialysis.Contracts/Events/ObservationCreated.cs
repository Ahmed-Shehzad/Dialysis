using BuildingBlocks;
using BuildingBlocks.Abstractions;
using Dialysis.Contracts.Ids;
using Intercessor.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Integration event raised when a FHIR Observation resource is created.
/// Uses strong ID types to avoid primitive obsession.
/// </summary>
public sealed record ObservationCreated(
    Ulid CorrelationId,
    string? TenantId,
    ObservationId ObservationId,
    PatientId PatientId,
    EncounterId EncounterId,
    string Code,
    string Value,
    DateTimeOffset Effective,
    string? DeviceId
) : IntegrationEvent(CorrelationId), INotification;
