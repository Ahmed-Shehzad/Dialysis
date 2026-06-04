using System.Security.Cryptography.X509Certificates;

namespace Dialysis.BuildingBlocks.Fhir.Tefca;

public interface ITefcaTrustAnchorValidator
{
    bool Validate(X509Certificate2 partnerCert);
}

public sealed class TefcaTrustAnchorValidator : ITefcaTrustAnchorValidator
{
    private readonly IReadOnlyCollection<X509Certificate2> _trustBundle;
    public TefcaTrustAnchorValidator(IReadOnlyCollection<X509Certificate2> trustBundle) => _trustBundle = trustBundle;
    public bool Validate(X509Certificate2 partnerCert)
    {
        ArgumentNullException.ThrowIfNull(partnerCert);
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // operational follow-up: real revocation
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        foreach (var anchor in _trustBundle)
        {
            chain.ChainPolicy.CustomTrustStore.Add(anchor);
        }
        return chain.Build(partnerCert);
    }
}

public sealed record TefcaPartnerPolicy
{
    public TefcaPartnerPolicy(string PartnerId,
        bool RequireIas,
        IReadOnlyList<string> AllowedPurposes,
        string? MaxAudience)
    {
        this.PartnerId = PartnerId;
        this.RequireIas = RequireIas;
        this.AllowedPurposes = AllowedPurposes;
        this.MaxAudience = MaxAudience;
    }
    public string PartnerId { get; init; }
    public bool RequireIas { get; init; }
    public IReadOnlyList<string> AllowedPurposes { get; init; }
    public string? MaxAudience { get; init; }
    public void Deconstruct(out string PartnerId, out bool RequireIas, out IReadOnlyList<string> AllowedPurposes, out string? MaxAudience)
    {
        PartnerId = this.PartnerId;
        RequireIas = this.RequireIas;
        AllowedPurposes = this.AllowedPurposes;
        MaxAudience = this.MaxAudience;
    }
}
