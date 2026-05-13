using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Contracts.Integration;

public sealed record DialysisSessionStartedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid SessionId,
    Guid PatientId,
    DateTime StartedAtUtc,
    string DialyzerModel,
    int BloodFlowRateMlPerMin) : IIntegrationEvent;

public sealed record DialysisSessionCompletedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid SessionId,
    Guid PatientId,
    DateTime CompletedAtUtc,
    int ActualDurationMinutes,
    decimal AchievedUfVolumeLiters) : IIntegrationEvent;

public sealed record DialysisSessionAbortedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid SessionId,
    Guid PatientId,
    DateTime AbortedAtUtc,
    string ReasonCode) : IIntegrationEvent;

public sealed record IntradialyticAdverseEventIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid SessionId,
    Guid PatientId,
    DateTime ObservedAtUtc,
    string EventKindCode,
    string Severity,
    string? Notes) : IIntegrationEvent;
