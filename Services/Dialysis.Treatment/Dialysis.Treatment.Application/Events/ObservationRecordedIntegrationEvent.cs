using BuildingBlocks;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Events;

/// <summary>
/// Integration event published when a device observation is recorded.
/// Consumable by downstream services (analytics, audit, etc.) via Transponder.
/// </summary>
public sealed record ObservationRecordedIntegrationEvent(
    Ulid TreatmentSessionId,
    string SessionId,
    Ulid ObservationId,
    ObservationCode Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? ChannelName,
    string? TenantId) : IntegrationEvent(TreatmentSessionId);
