using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dialysis.SmartConnect.Adapters.Epic;

public sealed class EpicAdapterOptions
{
    /// <summary>FHIR R4 base URL, e.g. <c>https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4</c>.</summary>
    public required string BaseUrl { get; set; }

    /// <summary>OAuth2 token endpoint, e.g. <c>https://fhir.epic.com/interconnect-fhir-oauth/oauth2/token</c>.</summary>
    public required string TokenEndpoint { get; set; }

    /// <summary>Epic-issued non-production or production client identifier.</summary>
    public required string ClientId { get; set; }

    /// <summary>Path to the PEM-encoded RSA private key registered with Epic for JWT-bearer client assertion.</summary>
    public required string PrivateKeyPemPath { get; set; }

    /// <summary>Space-delimited scopes (Epic Backend Services typically use <c>system/*.read</c>).</summary>
    public string Scope { get; set; } = "system/*.read";

    /// <summary>Lifetime of the signed client-assertion JWT. Epic accepts up to 5 minutes.</summary>
    public TimeSpan ClientAssertionLifetime { get; set; } = TimeSpan.FromMinutes(4);
}

/// <summary>
/// Epic-on-FHIR backend-services auth via signed JWT client assertion (RS384). The signed assertion
/// is exchanged at the token endpoint for a bearer access token; tokens are cached per-tenant until
/// shortly before expiry.
/// </summary>
public sealed class EpicAuthProvider : IExternalEhrAuthProvider
{
    private readonly EpicAdapterOptions _options;
    private readonly Lazy<RsaSecurityKey> _signingKey;
    private readonly OAuth2TokenAcquirer _tokenAcquirer;
    /// <summary>
    /// Epic-on-FHIR backend-services auth via signed JWT client assertion (RS384). The signed assertion
    /// is exchanged at the token endpoint for a bearer access token; tokens are cached per-tenant until
    /// shortly before expiry.
    /// </summary>
    public EpicAuthProvider(IOptions<EpicAdapterOptions> options, OAuth2TokenAcquirer tokenAcquirer)
    {
        _tokenAcquirer = tokenAcquirer;
        _options = options.Value;
        _signingKey = new Lazy<RsaSecurityKey>(() => LoadRsaSigningKey(options.Value), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string VendorName => "Epic";

    public Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken)
    {
        var assertion = BuildClientAssertion();
        var request = new OAuth2TokenRequest(
            VendorName: VendorName,
            TokenEndpoint: _options.TokenEndpoint,
            CacheKey: $"epic-token::{context.TenantId}",
            FormFields:
            [
                new("grant_type", "client_credentials"),
                new("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new("client_assertion", assertion),
                new("scope", _options.Scope),
            ]);
        return _tokenAcquirer.AcquireAsync(request, cancellationToken);
    }

    private string BuildClientAssertion()
    {
        var now = DateTime.UtcNow;
        var signingCredentials = new SigningCredentials(_signingKey.Value, SecurityAlgorithms.RsaSha384);
        var jwt = new JwtSecurityToken(
            issuer: _options.ClientId,
            audience: _options.TokenEndpoint,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, _options.ClientId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ],
            notBefore: now,
            expires: now.Add(_options.ClientAssertionLifetime),
            signingCredentials: signingCredentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static RsaSecurityKey LoadRsaSigningKey(EpicAdapterOptions options)
    {
        var pem = File.ReadAllText(options.PrivateKeyPemPath);
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa) { KeyId = options.ClientId };
    }
}

public sealed class EpicFhirAdapter : HttpFhirAdapterBase
{
    private readonly EpicAdapterOptions _options;
    public EpicFhirAdapter(IHttpClientFactory httpClientFactory,
        EpicAuthProvider authProvider,
        IOptions<EpicAdapterOptions> options) : base(httpClientFactory, authProvider) =>
        _options = options.Value;

    public override ExternalEhrAdapterDescriptor Describe() => new("Epic", FhirVersion: "4.0.1", BaseUrl: _options.BaseUrl);
}
