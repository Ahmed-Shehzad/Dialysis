using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Registration.Domain;

public enum ContactSystem
{
    Phone = 1,
    Email = 2,
    Fax = 3,
    Sms = 4,
}

public enum ContactUse
{
    Home = 1,
    Work = 2,
    Mobile = 3,
    Old = 4,
}

public sealed class ContactPoint : ValueObject
{
    public ContactSystem System { get; }

    public string Value { get; }

    public ContactUse Use { get; }

    public ContactPoint(ContactSystem system, string value, ContactUse use)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        System = system;
        Value = value.Trim();
        Use = use;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return System;
        yield return Value;
        yield return Use;
    }
}
