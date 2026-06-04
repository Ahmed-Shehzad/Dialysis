using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.RegisterPatient;

public sealed record RegisterPatientCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterPatientCommand(string MedicalRecordNumber,
        string FamilyName,
        string GivenName,
        string? MiddleName,
        DateOnly DateOfBirth,
        string? SexAtBirthCode,
        string? PreferredLanguageCode)
    {
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.MiddleName = MiddleName;
        this.DateOfBirth = DateOfBirth;
        this.SexAtBirthCode = SexAtBirthCode;
        this.PreferredLanguageCode = PreferredLanguageCode;
    }
    public string RequiredPermission => EhrPermissions.PatientRegister;
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public string? MiddleName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string? SexAtBirthCode { get; init; }
    public string? PreferredLanguageCode { get; init; }
    public void Deconstruct(out string MedicalRecordNumber, out string FamilyName, out string GivenName, out string? MiddleName, out DateOnly DateOfBirth, out string? SexAtBirthCode, out string? PreferredLanguageCode)
    {
        MedicalRecordNumber = this.MedicalRecordNumber;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        MiddleName = this.MiddleName;
        DateOfBirth = this.DateOfBirth;
        SexAtBirthCode = this.SexAtBirthCode;
        PreferredLanguageCode = this.PreferredLanguageCode;
    }
}
