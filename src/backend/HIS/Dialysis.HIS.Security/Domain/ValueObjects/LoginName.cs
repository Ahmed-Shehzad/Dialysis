using System.Text.RegularExpressions;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Security.Domain.ValueObjects;

public sealed partial class LoginName : ValueObject
{
    [GeneratedRegex("^[a-z0-9._-]{3,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string Value { get; }

    public LoginName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("LoginName cannot be empty.");
        var trimmed = value.Trim();
        if (!Pattern().IsMatch(trimmed))
            throw new DomainException("LoginName must be 3–64 chars of lowercase alphanumerics, dot, underscore or hyphen.");
        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
