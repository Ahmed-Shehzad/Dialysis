using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>
/// Published by EHR.Billing once a completed dialysis session has been priced into a
/// <c>Charge</c>. HIE Documents consumes this and renders the itemised, AcroForm-fillable
/// invoice PDF. EHR owns the money; HIE owns the document artifact.
/// </summary>
public sealed record DialysisInvoiceReadyIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Published by EHR.Billing once a completed dialysis session has been priced into a
    /// <c>Charge</c>. HIE Documents consumes this and renders the itemised, AcroForm-fillable
    /// invoice PDF.
    /// </summary>
    public DialysisInvoiceReadyIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid ChargeId,
        Guid PatientId,
        Guid SessionId,
        string InvoiceNumber,
        DateTime IssueDateUtc,
        string Modality,
        string CptCode,
        int DurationMinutes,
        decimal Total,
        string CurrencyCode,
        IReadOnlyList<InvoiceLineDto> Lines)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.ChargeId = ChargeId;
        this.PatientId = PatientId;
        this.SessionId = SessionId;
        this.InvoiceNumber = InvoiceNumber;
        this.IssueDateUtc = IssueDateUtc;
        this.Modality = Modality;
        this.CptCode = CptCode;
        this.DurationMinutes = DurationMinutes;
        this.Total = Total;
        this.CurrencyCode = CurrencyCode;
        this.Lines = Lines;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    /// <summary>The captured charge this invoice bills.</summary>
    public Guid ChargeId { get; init; }
    public Guid PatientId { get; init; }
    /// <summary>The PDMS session / EHR encounter the invoice is for.</summary>
    public Guid SessionId { get; init; }
    /// <summary>Human-readable invoice number printed on the PDF.</summary>
    public string InvoiceNumber { get; init; }
    public DateTime IssueDateUtc { get; init; }
    public string Modality { get; init; }
    public string CptCode { get; init; }
    /// <summary>Machine usage time (treatment minutes, actual start → completion) printed on the invoice.</summary>
    public int DurationMinutes { get; init; }
    public decimal Total { get; init; }
    public string CurrencyCode { get; init; }
    /// <summary>Itemised charge lines rendered on the invoice.</summary>
    public IReadOnlyList<InvoiceLineDto> Lines { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid ChargeId, out Guid PatientId, out Guid SessionId, out string InvoiceNumber, out DateTime IssueDateUtc, out string Modality, out string CptCode, out int DurationMinutes, out decimal Total, out string CurrencyCode, out IReadOnlyList<InvoiceLineDto> Lines)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        ChargeId = this.ChargeId;
        PatientId = this.PatientId;
        SessionId = this.SessionId;
        InvoiceNumber = this.InvoiceNumber;
        IssueDateUtc = this.IssueDateUtc;
        Modality = this.Modality;
        CptCode = this.CptCode;
        DurationMinutes = this.DurationMinutes;
        Total = this.Total;
        CurrencyCode = this.CurrencyCode;
        Lines = this.Lines;
    }
}

/// <summary>One itemised invoice line carried on <see cref="DialysisInvoiceReadyIntegrationEvent"/>.</summary>
public sealed record InvoiceLineDto
{
    /// <summary>One itemised invoice line carried on <see cref="DialysisInvoiceReadyIntegrationEvent"/>.</summary>
    public InvoiceLineDto(string Label, decimal Quantity, string Unit, decimal UnitPrice, decimal Amount)
    {
        this.Label = Label;
        this.Quantity = Quantity;
        this.Unit = Unit;
        this.UnitPrice = UnitPrice;
        this.Amount = Amount;
    }
    public string Label { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public void Deconstruct(out string Label, out decimal Quantity, out string Unit, out decimal UnitPrice, out decimal Amount)
    {
        Label = this.Label;
        Quantity = this.Quantity;
        Unit = this.Unit;
        UnitPrice = this.UnitPrice;
        Amount = this.Amount;
    }
}
