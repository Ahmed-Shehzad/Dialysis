using BuildingBlocks;
using BuildingBlocks.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record TreatmentSessionStartedFhirNotifyEvent(
    Ulid TreatmentSessionId,
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId) : DomainEvent;
