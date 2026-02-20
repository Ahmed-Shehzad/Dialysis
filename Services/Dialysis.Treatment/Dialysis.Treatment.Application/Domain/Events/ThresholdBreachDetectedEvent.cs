using BuildingBlocks;

using Dialysis.Treatment.Application.Domain.Services;
using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Events;

/// <summary>
/// Raised when VitalSignsMonitoringService detects an observation value outside clinical thresholds.
/// </summary>
public sealed record ThresholdBreachDetectedEvent(
    Ulid TreatmentSessionId,
    string SessionId,
    Ulid ObservationId,
    ObservationCode Code,
    ThresholdBreach Breach) : DomainEvent;
