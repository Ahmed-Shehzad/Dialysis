using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when a waiting patient is moved into a chair (treatment about to start).
/// PDMS is the primary consumer — pairs the chair identifier with an incoming dialysis
/// session so the chairside view can resolve patient context before vitals arrive.
/// </summary>
public sealed record PatientPlacedInChairIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid EntryId,
    Guid PatientId,
    string Chair,
    DateTime PlacedAtUtc) : IIntegrationEvent;
