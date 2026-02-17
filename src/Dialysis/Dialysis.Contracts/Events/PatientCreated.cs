using BuildingBlocks;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Raised when a patient is created. C5: Security-relevant action for audit.
/// </summary>
public sealed record PatientCreated(
    string LogicalId,
    TenantId TenantId
) : IntegrationEvent(Ulid.NewUlid()), INotification;
