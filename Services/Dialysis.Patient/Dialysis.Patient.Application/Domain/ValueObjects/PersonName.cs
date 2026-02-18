namespace Dialysis.Patient.Application.Domain.ValueObjects;

/// <summary>
/// Composite value object representing a person's name (first + last).
/// Aligns with HL7 PID-5 (Patient Name) XPN data type.
/// </summary>
public sealed record PersonName
{
    public string FirstName { get; }
    public string LastName { get; }

    public PersonName(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        FirstName = firstName;
        LastName = lastName;
    }

    public override string ToString() => $"{LastName}, {FirstName}";
}
