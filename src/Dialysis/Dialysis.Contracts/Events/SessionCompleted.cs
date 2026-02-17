using BuildingBlocks;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Raised when a dialysis session is completed and persisted.
/// Downstream handlers: push Procedure to EHR, record audit.
/// </summary>
public sealed record SessionCompleted(
    string SessionId,
    PatientId PatientId,
    TenantId TenantId
) : IntegrationEvent(Ulid.NewUlid()), INotification;
