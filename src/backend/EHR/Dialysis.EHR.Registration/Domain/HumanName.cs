using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Registration.Domain;

/// <summary>FHIR-aligned name parts. Used by both patient and provider aggregates.</summary>
public sealed class HumanName : ValueObject
{
    public string FamilyName { get; }

    public string GivenName { get; }

    public string? MiddleName { get; }

    public string? PrefixName { get; }

    public string? SuffixName { get; }

    public HumanName(string familyName, string givenName, string? middleName = null, string? prefixName = null, string? suffixName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(givenName);
        FamilyName = familyName.Trim();
        GivenName = givenName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        PrefixName = string.IsNullOrWhiteSpace(prefixName) ? null : prefixName.Trim();
        SuffixName = string.IsNullOrWhiteSpace(suffixName) ? null : suffixName.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FamilyName;
        yield return GivenName;
        yield return MiddleName;
        yield return PrefixName;
        yield return SuffixName;
    }
}
