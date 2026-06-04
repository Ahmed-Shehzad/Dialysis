namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// One patient row returned by the demographics resolver, ready for serialisation as a
/// <c>PID</c> segment inside the PDQ <c>RSP^K22^RSP_K21</c> response.
/// </summary>
public sealed record PdqMatch
{
    /// <summary>
    /// One patient row returned by the demographics resolver, ready for serialisation as a
    /// <c>PID</c> segment inside the PDQ <c>RSP^K22^RSP_K21</c> response.
    /// </summary>
    public PdqMatch(string MedicalRecordNumber,
        string FamilyName,
        string GivenName,
        DateOnly? DateOfBirth,
        string? SexAtBirthCode)
    {
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.DateOfBirth = DateOfBirth;
        this.SexAtBirthCode = SexAtBirthCode;
    }
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? SexAtBirthCode { get; init; }
    public void Deconstruct(out string MedicalRecordNumber, out string FamilyName, out string GivenName, out DateOnly? DateOfBirth, out string? SexAtBirthCode)
    {
        MedicalRecordNumber = this.MedicalRecordNumber;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        DateOfBirth = this.DateOfBirth;
        SexAtBirthCode = this.SexAtBirthCode;
    }
}
