using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record AppointmentBookedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid AppointmentId,
    Guid PatientId,
    Guid ProviderId,
    DateTime StartUtc,
    DateTime EndUtc,
    string EncounterClassCode,
    string? VisitReason) : IIntegrationEvent;

public sealed record AppointmentCancelledIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid AppointmentId,
    Guid PatientId,
    string ReasonCode) : IIntegrationEvent;

public sealed record AppointmentRescheduledIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid AppointmentId,
    Guid PatientId,
    DateTime NewStartUtc,
    DateTime NewEndUtc) : IIntegrationEvent;

public sealed record AppointmentCheckedInIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid AppointmentId,
    Guid PatientId,
    DateTime CheckedInAtUtc) : IIntegrationEvent;
