using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record ChargeCapturedIntegrationEvent : IIntegrationEvent
{
    public ChargeCapturedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid ChargeId,
        Guid PatientId,
        Guid EncounterId,
        string CptCode,
        IReadOnlyList<string> DiagnosisPointerIcd10Codes,
        decimal BilledAmount,
        string CurrencyCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.ChargeId = ChargeId;
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.CptCode = CptCode;
        this.DiagnosisPointerIcd10Codes = DiagnosisPointerIcd10Codes;
        this.BilledAmount = BilledAmount;
        this.CurrencyCode = CurrencyCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid ChargeId { get; init; }
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public string CptCode { get; init; }
    public IReadOnlyList<string> DiagnosisPointerIcd10Codes { get; init; }
    public decimal BilledAmount { get; init; }
    public string CurrencyCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid ChargeId, out Guid PatientId, out Guid EncounterId, out string CptCode, out IReadOnlyList<string> DiagnosisPointerIcd10Codes, out decimal BilledAmount, out string CurrencyCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        ChargeId = this.ChargeId;
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        CptCode = this.CptCode;
        DiagnosisPointerIcd10Codes = this.DiagnosisPointerIcd10Codes;
        BilledAmount = this.BilledAmount;
        CurrencyCode = this.CurrencyCode;
    }
}

public sealed record ClaimSubmittedIntegrationEvent : IIntegrationEvent
{
    public ClaimSubmittedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid ClaimId,
        Guid PatientId,
        Guid PayerId,
        string PayerCode,
        string ClaimFormatCode,
        decimal BilledTotal,
        string CurrencyCode,
        string ExternalControlNumber)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.ClaimId = ClaimId;
        this.PatientId = PatientId;
        this.PayerId = PayerId;
        this.PayerCode = PayerCode;
        this.ClaimFormatCode = ClaimFormatCode;
        this.BilledTotal = BilledTotal;
        this.CurrencyCode = CurrencyCode;
        this.ExternalControlNumber = ExternalControlNumber;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid ClaimId { get; init; }
    public Guid PatientId { get; init; }
    public Guid PayerId { get; init; }
    public string PayerCode { get; init; }
    public string ClaimFormatCode { get; init; }
    public decimal BilledTotal { get; init; }
    public string CurrencyCode { get; init; }
    public string ExternalControlNumber { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid ClaimId, out Guid PatientId, out Guid PayerId, out string PayerCode, out string ClaimFormatCode, out decimal BilledTotal, out string CurrencyCode, out string ExternalControlNumber)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        ClaimId = this.ClaimId;
        PatientId = this.PatientId;
        PayerId = this.PayerId;
        PayerCode = this.PayerCode;
        ClaimFormatCode = this.ClaimFormatCode;
        BilledTotal = this.BilledTotal;
        CurrencyCode = this.CurrencyCode;
        ExternalControlNumber = this.ExternalControlNumber;
    }
}

public sealed record RemittanceReceivedIntegrationEvent : IIntegrationEvent
{
    public RemittanceReceivedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid RemittanceId,
        Guid ClaimId,
        decimal PaidAmount,
        decimal AdjustmentAmount,
        string CurrencyCode,
        string PayerCode,
        string AdjudicationStatusCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.RemittanceId = RemittanceId;
        this.ClaimId = ClaimId;
        this.PaidAmount = PaidAmount;
        this.AdjustmentAmount = AdjustmentAmount;
        this.CurrencyCode = CurrencyCode;
        this.PayerCode = PayerCode;
        this.AdjudicationStatusCode = AdjudicationStatusCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid RemittanceId { get; init; }
    public Guid ClaimId { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal AdjustmentAmount { get; init; }
    public string CurrencyCode { get; init; }
    public string PayerCode { get; init; }
    public string AdjudicationStatusCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid RemittanceId, out Guid ClaimId, out decimal PaidAmount, out decimal AdjustmentAmount, out string CurrencyCode, out string PayerCode, out string AdjudicationStatusCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        RemittanceId = this.RemittanceId;
        ClaimId = this.ClaimId;
        PaidAmount = this.PaidAmount;
        AdjustmentAmount = this.AdjustmentAmount;
        CurrencyCode = this.CurrencyCode;
        PayerCode = this.PayerCode;
        AdjudicationStatusCode = this.AdjudicationStatusCode;
    }
}

public sealed record PaymentPostedIntegrationEvent : IIntegrationEvent
{
    public PaymentPostedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PaymentId,
        Guid PatientId,
        Guid? ClaimId,
        decimal Amount,
        string CurrencyCode,
        string PaymentMethodCode,
        DateTime PostedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PaymentId = PaymentId;
        this.PatientId = PatientId;
        this.ClaimId = ClaimId;
        this.Amount = Amount;
        this.CurrencyCode = CurrencyCode;
        this.PaymentMethodCode = PaymentMethodCode;
        this.PostedAtUtc = PostedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PaymentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid? ClaimId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; }
    public string PaymentMethodCode { get; init; }
    public DateTime PostedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PaymentId, out Guid PatientId, out Guid? ClaimId, out decimal Amount, out string CurrencyCode, out string PaymentMethodCode, out DateTime PostedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PaymentId = this.PaymentId;
        PatientId = this.PatientId;
        ClaimId = this.ClaimId;
        Amount = this.Amount;
        CurrencyCode = this.CurrencyCode;
        PaymentMethodCode = this.PaymentMethodCode;
        PostedAtUtc = this.PostedAtUtc;
    }
}

/// <summary>
/// Carries an inbound ANSI ASC X12N ack payload (999 functional ack or 277CA claim ack)
/// from SmartConnect into EHR.Billing. SmartConnect inspects the ISA/GS header to detect
/// the ack kind and emits this event with the raw byte payload so the EHR side parses it
/// the same way regardless of which clearinghouse delivered it.
/// </summary>
public sealed record EdiAcknowledgementReceivedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Carries an inbound ANSI ASC X12N ack payload (999 functional ack or 277CA claim ack)
    /// from SmartConnect into EHR.Billing. SmartConnect inspects the ISA/GS header to detect
    /// the ack kind and emits this event with the raw byte payload so the EHR side parses it
    /// the same way regardless of which clearinghouse delivered it.
    /// </summary>
    public EdiAcknowledgementReceivedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        EdiAckKind AckKind,
        byte[] PayloadBytes,
        DateTime ReceivedAtUtc,
        string? SourceTrace)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AckKind = AckKind;
        this.PayloadBytes = PayloadBytes;
        this.ReceivedAtUtc = ReceivedAtUtc;
        this.SourceTrace = SourceTrace;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public EdiAckKind AckKind { get; init; }
    public byte[] PayloadBytes { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public string? SourceTrace { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out EdiAckKind AckKind, out byte[] PayloadBytes, out DateTime ReceivedAtUtc, out string? SourceTrace)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AckKind = this.AckKind;
        PayloadBytes = this.PayloadBytes;
        ReceivedAtUtc = this.ReceivedAtUtc;
        SourceTrace = this.SourceTrace;
    }
}

public enum EdiAckKind
{
    FunctionalAck999 = 0,
    ClaimAck277Ca = 1,
}
