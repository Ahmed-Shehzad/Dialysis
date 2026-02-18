using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record TreatmentSessionStartedEvent(
    Ulid TreatmentSessionId,
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId) : DomainEvent;
