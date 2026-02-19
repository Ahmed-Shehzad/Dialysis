using BuildingBlocks;
using BuildingBlocks.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record TreatmentSessionStartedEvent(
    Ulid TreatmentSessionId,
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId) : DomainEvent;
