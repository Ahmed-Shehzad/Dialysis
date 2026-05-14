using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record PatientPortalAppointmentRequestedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid RequestId,
    Guid PatientId,
    string ReasonText,
    DateTime EarliestPreferredUtc,
    DateTime LatestPreferredUtc) : IIntegrationEvent;

public sealed record PatientPortalSecureMessageSentIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid MessageId,
    Guid PatientId,
    Guid? TargetProviderId,
    string Subject) : IIntegrationEvent;
