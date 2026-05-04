using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents;

public sealed record ReferralCreatedIntegrationEvent(
    Guid ReferralId,
    Guid PatientId,
    string ReferralTypeCode,
    DateTime CreatedAtUtc)
    : IntegrationEvent;
