using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dialysis.HIE.Tefca.Ias;

/// <summary>Options for the v1 HMAC-SHA-256 IAS issuer.</summary>
public sealed class IasJwtIssuerOptions
{
    /// <summary>
    /// Shared secret used for the HMAC signature. Operators configure this per-deployment
    /// (typically through a secret store). At least 32 bytes when UTF-8-encoded.
    /// </summary>
    public string? SigningKey { get; set; }
}

/// <summary>
/// HMAC-SHA-256 <see cref="IIasJwtIssuer"/>. Issues a JWT with the standard TEFCA IAS
/// claims (<c>iss</c>, <c>sub</c>, <c>aud</c>, <c>scope</c>, <c>iat</c>, <c>exp</c>,
/// <c>jti</c>). Production deployments swap for an RS256 variant signed by the platform
/// cert; the issuer interface stays the same.
/// </summary>
public sealed class HmacIasJwtIssuer : IIasJwtIssuer
{
    private const string IssuerName = "DialysisPlatform.Tefca";
    private readonly IasJwtIssuerOptions _options;
    private readonly TimeProvider _clock;

    public HmacIasJwtIssuer(IOptions<IasJwtIssuerOptions> options, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _options = options.Value;
        _clock = clock;
    }

    public string Issue(IasJwtRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Scope);
        if (request.Lifetime <= TimeSpan.Zero)
            throw new ArgumentException("Lifetime must be positive.", nameof(request));
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException(
                "Tefca:IasJwtIssuer:SigningKey is not configured; cannot mint an IAS JWT.");
        }

        var now = _clock.GetUtcNow();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Subject),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString("N")),
            new("scope", request.Scope),
            new("tefca_role", "qhin"),
            new("originator", IssuerName),
        };
        // TEFCA permitted purpose travels as the standard purpose_of_use claim so the partner can
        // enforce it. Omitted on the purpose-agnostic admin preview/verification path.
        if (!string.IsNullOrWhiteSpace(request.PurposeOfUse))
        {
            claims.Add(new Claim("purpose_of_use", request.PurposeOfUse));
        }
        var jwt = new JwtSecurityToken(
            issuer: request.Issuer,
            audience: request.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(request.Lifetime).UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
