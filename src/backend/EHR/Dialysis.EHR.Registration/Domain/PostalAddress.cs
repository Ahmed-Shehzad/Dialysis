using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Registration.Domain;

public sealed class PostalAddress : ValueObject
{
    public string Line1 { get; }

    public string? Line2 { get; }

    public string City { get; }

    public string StateOrProvince { get; }

    public string PostalCode { get; }

    public string CountryCode { get; }

    public PostalAddress(string line1, string city, string stateOrProvince, string postalCode, string countryCode, string? line2 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateOrProvince);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        Line1 = line1.Trim();
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = city.Trim();
        StateOrProvince = stateOrProvince.Trim();
        PostalCode = postalCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return StateOrProvince;
        yield return PostalCode;
        yield return CountryCode;
    }
}
