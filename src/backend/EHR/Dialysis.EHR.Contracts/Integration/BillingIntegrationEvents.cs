using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record ChargeCapturedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
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
    int SchemaVersion,
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
    int SchemaVersion,
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
    int SchemaVersion,
    Guid PaymentId,
    Guid PatientId,
    Guid? ClaimId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethodCode,
    DateTime PostedAtUtc) : IIntegrationEvent;

/// <summary>
/// Carries an inbound ANSI ASC X12N ack payload (999 functional ack or 277CA claim ack)
/// from SmartConnect into EHR.Billing. SmartConnect inspects the ISA/GS header to detect
/// the ack kind and emits this event with the raw byte payload so the EHR side parses it
/// the same way regardless of which clearinghouse delivered it.
/// </summary>
public sealed record EdiAcknowledgementReceivedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    EdiAckKind AckKind,
    byte[] PayloadBytes,
    DateTime ReceivedAtUtc,
    string? SourceTrace) : IIntegrationEvent;

public enum EdiAckKind
{
    FunctionalAck999 = 0,
    ClaimAck277Ca = 1,
}
