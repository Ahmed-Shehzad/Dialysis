using BuildingBlocks;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Raised when a dialysis session is started. C5: Security-relevant action for audit.
/// </summary>
public sealed record SessionStarted(
    string SessionId,
    PatientId PatientId,
    TenantId TenantId
) : IntegrationEvent(Ulid.NewUlid()), INotification;
