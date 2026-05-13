using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.UpdatePatientDemographics;

public sealed record UpdatePatientDemographicsCommand(
    Guid PatientId,
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
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientUpdate;
}
