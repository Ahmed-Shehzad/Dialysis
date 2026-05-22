namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// One patient row returned by the demographics resolver, ready for serialisation as a
/// <c>PID</c> segment inside the PDQ <c>RSP^K22^RSP_K21</c> response.
/// </summary>
public sealed record PdqMatch(
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    DateOnly? DateOfBirth,
    string? SexAtBirthCode);
