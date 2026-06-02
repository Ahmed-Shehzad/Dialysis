using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Contracts.Integration;

/// <summary>
/// Published when a completed dialysis session is ready to be billed. EHR.Billing consumes
/// this and creates a <c>Charge</c> with the appropriate CPT code (90935 / 90937 HD,
/// 90945 / 90947 PD). The CPT mapping is resolved by the producer so EHR.Billing never has
/// to model PDMS modality semantics.
/// </summary>
public sealed record DialysisSessionChargeReadyIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid SessionId,
    Guid PatientId,
    string Modality,
    int DurationMinutes,
    DateTime CompletedAtUtc,
    string CptCode) : IIntegrationEvent;
