using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Dialysis.BuildingBlocks.Fhir.Tefca;

public sealed record TefcaAssertionInput
{
    public TefcaAssertionInput(string Issuer,
        string Audience,
        string PurposeOfUse,
        string RequestingOrganization,
        string TreatingProviderOrganization,
        string? TreatingProviderIndividual,
        string SubjectRole,
        string HomeCommunityId,
        TimeSpan Lifetime)
    {
        this.Issuer = Issuer;
        this.Audience = Audience;
        this.PurposeOfUse = PurposeOfUse;
        this.RequestingOrganization = RequestingOrganization;
        this.TreatingProviderOrganization = TreatingProviderOrganization;
        this.TreatingProviderIndividual = TreatingProviderIndividual;
        this.SubjectRole = SubjectRole;
        this.HomeCommunityId = HomeCommunityId;
        this.Lifetime = Lifetime;
    }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public string PurposeOfUse { get; init; }
    public string RequestingOrganization { get; init; }
    public string TreatingProviderOrganization { get; init; }
    public string? TreatingProviderIndividual { get; init; }
    public string SubjectRole { get; init; }
    public string HomeCommunityId { get; init; }
    public TimeSpan Lifetime { get; init; }
    public void Deconstruct(out string Issuer, out string Audience, out string PurposeOfUse, out string RequestingOrganization, out string TreatingProviderOrganization, out string? TreatingProviderIndividual, out string SubjectRole, out string HomeCommunityId, out TimeSpan Lifetime)
    {
        Issuer = this.Issuer;
        Audience = this.Audience;
        PurposeOfUse = this.PurposeOfUse;
        RequestingOrganization = this.RequestingOrganization;
        TreatingProviderOrganization = this.TreatingProviderOrganization;
        TreatingProviderIndividual = this.TreatingProviderIndividual;
        SubjectRole = this.SubjectRole;
        HomeCommunityId = this.HomeCommunityId;
        Lifetime = this.Lifetime;
    }
}

public interface ITefcaIdentityAssertionBuilder
{
    string Build(TefcaAssertionInput input);
}

public sealed class TefcaIdentityAssertionBuilder : ITefcaIdentityAssertionBuilder
{
    private readonly X509Certificate2 _signingCertificate;
    public TefcaIdentityAssertionBuilder(X509Certificate2 signingCertificate) => _signingCertificate = signingCertificate;
    public string Build(TefcaAssertionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var signingCredentials = new X509SigningCredentials(_signingCertificate, SecurityAlgorithms.RsaSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: input.Issuer,
            audience: input.Audience,
            claims:
            [
                new Claim("purpose_of_use", input.PurposeOfUse),
                new Claim("requesting_organization", input.RequestingOrganization),
                new Claim("treating_provider_org", input.TreatingProviderOrganization),
                new Claim("treating_provider_individual", input.TreatingProviderIndividual ?? string.Empty),
                new Claim("subject_role", input.SubjectRole),
                new Claim("home_community_id", input.HomeCommunityId),
            ],
            notBefore: now,
            expires: now.Add(input.Lifetime),
            signingCredentials: signingCredentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
