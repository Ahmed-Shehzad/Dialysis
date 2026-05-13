using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record ChargeCapturedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid ChargeId,
    Guid PatientId,
    Guid EncounterId,
    string CptCode,
    IReadOnlyList<string> DiagnosisPointerIcd10Codes,
    decimal BilledAmount,
    string CurrencyCode) : IIntegrationEvent;

public sealed record ClaimSubmittedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid ClaimId,
    Guid PatientId,
    Guid PayerId,
    string PayerCode,
    string ClaimFormatCode,
    decimal BilledTotal,
    string CurrencyCode,
    string ExternalControlNumber) : IIntegrationEvent;

public sealed record RemittanceReceivedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid RemittanceId,
    Guid ClaimId,
    decimal PaidAmount,
    decimal AdjustmentAmount,
    string CurrencyCode,
    string PayerCode,
    string AdjudicationStatusCode) : IIntegrationEvent;

public sealed record PaymentPostedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid PaymentId,
    Guid PatientId,
    Guid? ClaimId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethodCode,
    DateTime PostedAtUtc) : IIntegrationEvent;
