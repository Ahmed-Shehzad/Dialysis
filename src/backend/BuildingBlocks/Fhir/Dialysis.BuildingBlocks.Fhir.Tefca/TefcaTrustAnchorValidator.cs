using System.Security.Cryptography.X509Certificates;

namespace Dialysis.BuildingBlocks.Fhir.Tefca;

public interface ITefcaTrustAnchorValidator
{
    bool Validate(X509Certificate2 partnerCert);
}

public sealed class TefcaTrustAnchorValidator(IReadOnlyCollection<X509Certificate2> trustBundle) : ITefcaTrustAnchorValidator
{
    public bool Validate(X509Certificate2 partnerCert)
    {
        ArgumentNullException.ThrowIfNull(partnerCert);
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // operational follow-up: real revocation
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        foreach (var anchor in trustBundle)
        {
            chain.ChainPolicy.CustomTrustStore.Add(anchor);
        }
        return chain.Build(partnerCert);
    }
}

public sealed record TefcaPartnerPolicy(
    string PartnerId,
    bool RequireIas,
    IReadOnlyList<string> AllowedPurposes,
    string? MaxAudience);
