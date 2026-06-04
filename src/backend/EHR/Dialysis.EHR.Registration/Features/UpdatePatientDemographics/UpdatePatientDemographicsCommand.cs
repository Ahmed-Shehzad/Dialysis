using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.UpdatePatientDemographics;

public sealed record UpdatePatientDemographicsCommand : ICommand, IPermissionedCommand
{
    public UpdatePatientDemographicsCommand(Guid PatientId,
        string FamilyName,
        string GivenName,
        string? MiddleName,
        string? SexAtBirthCode,
        string? PreferredLanguageCode,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? StateOrProvince,
        string? PostalCode,
        string? CountryCode)
    {
        this.PatientId = PatientId;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.MiddleName = MiddleName;
        this.SexAtBirthCode = SexAtBirthCode;
        this.PreferredLanguageCode = PreferredLanguageCode;
        this.AddressLine1 = AddressLine1;
        this.AddressLine2 = AddressLine2;
        this.City = City;
        this.StateOrProvince = StateOrProvince;
        this.PostalCode = PostalCode;
        this.CountryCode = CountryCode;
    }
    public string RequiredPermission => EhrPermissions.PatientUpdate;
    public Guid PatientId { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public string? MiddleName { get; init; }
    public string? SexAtBirthCode { get; init; }
    public string? PreferredLanguageCode { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateOrProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public void Deconstruct(out Guid PatientId, out string FamilyName, out string GivenName, out string? MiddleName, out string? SexAtBirthCode, out string? PreferredLanguageCode, out string? AddressLine1, out string? AddressLine2, out string? City, out string? StateOrProvince, out string? PostalCode, out string? CountryCode)
    {
        PatientId = this.PatientId;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        MiddleName = this.MiddleName;
        SexAtBirthCode = this.SexAtBirthCode;
        PreferredLanguageCode = this.PreferredLanguageCode;
        AddressLine1 = this.AddressLine1;
        AddressLine2 = this.AddressLine2;
        City = this.City;
        StateOrProvince = this.StateOrProvince;
        PostalCode = this.PostalCode;
        CountryCode = this.CountryCode;
    }
}
