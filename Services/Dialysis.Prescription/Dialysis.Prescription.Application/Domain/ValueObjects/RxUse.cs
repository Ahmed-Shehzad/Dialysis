namespace Dialysis.Prescription.Application.Domain.ValueObjects;

/// <summary>
/// Prescription parameter Use from PCD-01 Table 2 (HL7 Dialysis Implementation Guide).
/// M = Mandatory (prescription-eligible), C = Conditional, O = Optional.
/// </summary>
public enum RxUse
{
    /// <summary>Mandatory – required for prescription.</summary>
    M,

    /// <summary>Conditional – required when applicable (e.g. C10, C11).</summary>
    C,

    /// <summary>Optional – not required for prescription.</summary>
    O
}
