using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Raised when a new FHIR Observation (e.g. vitals) is created and persisted.
/// </summary>
public sealed record ObservationCreated(
    ObservationId ObservationId,
    PatientId PatientId,
    TenantId TenantId,
    LoincCode LoincCode,
    UnitOfMeasure? Unit,
    decimal? NumericValue,
    ObservationEffective Effective
) : IntegrationEvent(Ulid.NewUlid()), INotification;
