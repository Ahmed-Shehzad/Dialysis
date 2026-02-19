using BuildingBlocks;

namespace Dialysis.Device.Application.Domain.Events;

public sealed record DeviceRegisteredEvent(
    Ulid DeviceId,
    string DeviceEui64,
    string? Manufacturer,
    string? Model) : DomainEvent;
