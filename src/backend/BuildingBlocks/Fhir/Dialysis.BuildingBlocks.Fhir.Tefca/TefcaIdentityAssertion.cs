using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Dialysis.BuildingBlocks.Fhir.Tefca;

public sealed record TefcaAssertionInput(
    string Issuer,
    string Audience,
    string PurposeOfUse,
    string RequestingOrganization,
    string TreatingProviderOrganization,
    string? TreatingProviderIndividual,
    string SubjectRole,
    string HomeCommunityId,
    TimeSpan Lifetime);

public interface ITefcaIdentityAssertionBuilder
{
    string Build(TefcaAssertionInput input);
}

public sealed class TefcaIdentityAssertionBuilder(X509Certificate2 signingCertificate) : ITefcaIdentityAssertionBuilder
{
    public string Build(TefcaAssertionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var signingCredentials = new X509SigningCredentials(signingCertificate, SecurityAlgorithms.RsaSha256);
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
