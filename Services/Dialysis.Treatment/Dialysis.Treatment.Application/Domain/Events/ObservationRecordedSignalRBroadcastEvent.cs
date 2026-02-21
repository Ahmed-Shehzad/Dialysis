using BuildingBlocks;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record ObservationRecordedSignalRBroadcastEvent(
    Ulid TreatmentSessionId,
    string SessionId,
    Ulid ObservationId,
    ObservationCode Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? ChannelName = null) : DomainEvent;
