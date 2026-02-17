using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Raised when hypotension risk is detected from vitals (e.g. low BP).
/// </summary>
public sealed record HypotensionRiskRaised(
    ObservationId ObservationId,
    PatientId PatientId,
    TenantId TenantId,
    string Reason,
    decimal? Systolic,
    decimal? Diastolic
) : IntegrationEvent(Ulid.NewUlid()), INotification;
