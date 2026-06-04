using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.RegisterProvider;

public sealed record RegisterProviderCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterProviderCommand(string NationalProviderIdentifier,
        string FamilyName,
        string GivenName,
        ProviderKind Kind,
        string? SpecialtyCode,
        string? LicenseNumber)
    {
        this.NationalProviderIdentifier = NationalProviderIdentifier;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.Kind = Kind;
        this.SpecialtyCode = SpecialtyCode;
        this.LicenseNumber = LicenseNumber;
    }
    public string RequiredPermission => EhrPermissions.PatientRegister;
    public string NationalProviderIdentifier { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public ProviderKind Kind { get; init; }
    public string? SpecialtyCode { get; init; }
    public string? LicenseNumber { get; init; }
    public void Deconstruct(out string NationalProviderIdentifier, out string FamilyName, out string GivenName, out ProviderKind Kind, out string? SpecialtyCode, out string? LicenseNumber)
    {
        NationalProviderIdentifier = this.NationalProviderIdentifier;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        Kind = this.Kind;
        SpecialtyCode = this.SpecialtyCode;
        LicenseNumber = this.LicenseNumber;
    }
}
