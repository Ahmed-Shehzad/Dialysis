using BuildingBlocks;

using Dialysis.Patient.Application.Domain.ValueObjects;

namespace Dialysis.Patient.Application.Domain.Events;

public sealed record PatientDemographicsUpdatedEvent(
    Ulid PatientId,
    PersonName Name) : DomainEvent;
