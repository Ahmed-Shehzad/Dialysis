namespace Dialysis.Lab.Contracts;

/// <summary>Clinical urgency of a lab order.</summary>
public enum LabOrderPriority
{
    Routine = 0,
    Stat = 1,
}

/// <summary>Lifecycle of a lab order: placed → transmitted to the LIS → in progress → resulted (or cancelled).</summary>
public enum LabOrderStatus
{
    Placed = 0,
    Transmitted = 1,
    InProgress = 2,
    Resulted = 3,
    Cancelled = 4,
}

/// <summary>HL7/FHIR-style result interpretation flag.</summary>
public enum LabResultInterpretation
{
    Normal = 0,
    Low = 1,
    High = 2,
    CriticalLow = 3,
    CriticalHigh = 4,
    Abnormal = 5,
}

/// <summary>One requested test on an order, identified by a LOINC code (the cross-context wire shape).</summary>
public sealed record LabTestRequestContract
{
    /// <summary>One requested test on an order, identified by a LOINC code.</summary>
    public LabTestRequestContract(string LoincCode, string Display)
    {
        this.LoincCode = LoincCode;
        this.Display = Display;
    }
    public string LoincCode { get; init; }
    public string Display { get; init; }
    public void Deconstruct(out string LoincCode, out string Display)
    {
        LoincCode = this.LoincCode;
        Display = this.Display;
    }
}

/// <summary>One observation in a returned result (the cross-context wire shape).</summary>
public sealed record LabObservationContract
{
    /// <summary>One observation in a returned result.</summary>
    public LabObservationContract(string LoincCode,
        string Display,
        string Value,
        string? Unit,
        string? ReferenceRange,
        LabResultInterpretation Interpretation)
    {
        this.LoincCode = LoincCode;
        this.Display = Display;
        this.Value = Value;
        this.Unit = Unit;
        this.ReferenceRange = ReferenceRange;
        this.Interpretation = Interpretation;
    }
    public string LoincCode { get; init; }
    public string Display { get; init; }
    public string Value { get; init; }
    public string? Unit { get; init; }
    public string? ReferenceRange { get; init; }
    public LabResultInterpretation Interpretation { get; init; }
    public void Deconstruct(out string LoincCode, out string Display, out string Value, out string? Unit, out string? ReferenceRange, out LabResultInterpretation Interpretation)
    {
        LoincCode = this.LoincCode;
        Display = this.Display;
        Value = this.Value;
        Unit = this.Unit;
        ReferenceRange = this.ReferenceRange;
        Interpretation = this.Interpretation;
    }
}
