using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.GetPatientById;

/// <summary>
/// Identity surface for one patient. Sized to what every consumer of the chart header
/// needs (display name, MRN, demographics, status) without dragging clinical sections in;
/// callers wanting allergies / problems / meds keep using <c>GET /patients/{id}/chart</c>.
/// </summary>
public sealed record PatientDetailDto
{
    /// <summary>
    /// Identity surface for one patient. Sized to what every consumer of the chart header
    /// needs (display name, MRN, demographics, status) without dragging clinical sections in;
    /// callers wanting allergies / problems / meds keep using <c>GET /patients/{id}/chart</c>.
    /// </summary>
    public PatientDetailDto(Guid Id,
        string MedicalRecordNumber,
        string FamilyName,
        string GivenName,
        string? MiddleName,
        DateOnly DateOfBirth,
        string? SexAtBirthCode,
        string? PreferredLanguageCode,
        string Status)
    {
        this.Id = Id;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.MiddleName = MiddleName;
        this.DateOfBirth = DateOfBirth;
        this.SexAtBirthCode = SexAtBirthCode;
        this.PreferredLanguageCode = PreferredLanguageCode;
        this.Status = Status;
    }
    public Guid Id { get; init; }
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public string? MiddleName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string? SexAtBirthCode { get; init; }
    public string? PreferredLanguageCode { get; init; }
    public string Status { get; init; }
    public void Deconstruct(out Guid id, out string medicalRecordNumber, out string familyName, out string givenName, out string? middleName, out DateOnly dateOfBirth, out string? sexAtBirthCode, out string? preferredLanguageCode, out string status)
    {
        id = this.Id;
        medicalRecordNumber = this.MedicalRecordNumber;
        familyName = this.FamilyName;
        givenName = this.GivenName;
        middleName = this.MiddleName;
        dateOfBirth = this.DateOfBirth;
        sexAtBirthCode = this.SexAtBirthCode;
        preferredLanguageCode = this.PreferredLanguageCode;
        status = this.Status;
    }
}

public sealed record GetPatientByIdQuery : IQuery<PatientDetailDto?>, IPermissionedCommand
{
    public GetPatientByIdQuery(Guid PatientId) => this.PatientId = PatientId;
    public string RequiredPermission => EhrPermissions.PatientRead;
    public Guid PatientId { get; init; }
    public void Deconstruct(out Guid patientId) => patientId = this.PatientId;
}
