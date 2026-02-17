using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Dialysis.Gateway.Infrastructure;

using System.IdentityModel.Tokens.Jwt;

namespace Dialysis.Gateway.Services;

/// <summary>
/// Issues JWTs for SMART on FHIR access tokens.
/// </summary>
public interface ISmartJwtIssuer
{
    string CreateAccessToken(string? clientId, string? scope, string? tenantId, int validMinutes = 60);
}

public sealed class SmartJwtIssuer : ISmartJwtIssuer
{
    private readonly SmartServerOptions _options;

    public SmartJwtIssuer(IOptions<SmartServerOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAccessToken(string? clientId, string? scope, string? tenantId, int validMinutes = 60)
    {
        var key = Convert.FromBase64String(_options.SigningKey!);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(clientId))
            claims.Add(new Claim("client_id", clientId));
        if (!string.IsNullOrEmpty(scope))
            claims.Add(new Claim("scope", scope));
        if (!string.IsNullOrEmpty(tenantId))
            claims.Add(new Claim("tenant_id", tenantId));

        var token = new JwtSecurityToken(
            issuer: _options.BaseUrl,
            audience: _options.BaseUrl,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(validMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
