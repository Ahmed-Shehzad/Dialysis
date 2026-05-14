using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Registration.Domain.ValueObjects;

/// <summary>
/// EHR-issued Medical Record Number. Trimmed; 1–64 chars. Equality is value-based per Evans VO rules.
/// </summary>
public sealed class MedicalRecordNumber : ValueObject
{
    public string Value { get; }

    public MedicalRecordNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("MedicalRecordNumber is required.");
        var normalized = value.Trim();
        if (normalized.Length > 64)
            throw new DomainException("MedicalRecordNumber must be 64 chars or fewer.");
        Value = normalized;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
