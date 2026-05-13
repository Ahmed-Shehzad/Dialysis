using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

/// <summary>FHIR-style coded value: <c>system + code + display</c>.</summary>
public sealed class Coding : ValueObject
{
    public string System { get; }

    public string Code { get; }

    public string? Display { get; }

    public Coding(string system, string code, string? display = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        System = system.Trim();
        Code = code.Trim();
        Display = string.IsNullOrWhiteSpace(display) ? null : display.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return System;
        yield return Code;
    }
}
