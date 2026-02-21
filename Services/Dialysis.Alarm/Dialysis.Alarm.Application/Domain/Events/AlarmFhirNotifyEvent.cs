using BuildingBlocks;

namespace Dialysis.Alarm.Application.Domain.Events;

/// <summary>
/// Raised when an alarm is raised; triggers FHIR subscription notification (DetectedIssue).
/// </summary>
public sealed record AlarmFhirNotifyEvent(Ulid AlarmId) : DomainEvent;
