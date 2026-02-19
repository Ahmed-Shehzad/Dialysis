using BuildingBlocks;

namespace Dialysis.Device.Application.Domain.Events;

public sealed record DeviceDetailsUpdatedEvent(
    Ulid DeviceId,
    string DeviceEui64,
    string? Manufacturer,
    string? Model,
    string? Serial,
    string? Udi) : DomainEvent;
