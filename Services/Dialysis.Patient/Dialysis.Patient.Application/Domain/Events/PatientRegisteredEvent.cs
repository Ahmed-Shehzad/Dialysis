using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.ValueObjects;

namespace Dialysis.Patient.Application.Domain.Events;

public sealed record PatientRegisteredEvent(
    Ulid PatientId,
    MedicalRecordNumber MedicalRecordNumber,
    Person Name) : DomainEvent;
