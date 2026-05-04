using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Dialysis.HIS.Api;
using Dialysis.HIS.Contracts.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Dialysis.HIS.Tests;

/// <summary>JWT + least-privilege: missing HIS permissions return HTTP 403 from the global exception handler.</summary>
public sealed class HisApiJwtBearerEnforcedFactory : HisApiWebApplicationFactoryBase
{
    public const string TestIssuer = "https://localhost/his-jwt-test-issuer";

    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("01234567890123456789012345678901"));

    protected override void ConfigureHisTestWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("His:Authentication:Authority", TestIssuer);
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.Authority = string.Empty;
                o.MetadataAddress = string.Empty;
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey,
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
            });
        });
    }

    internal static string CreateBearerToken(params Claim[] claims)
    {
        var creds = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class HisJwtAuthorizationIntegrationTests : IClassFixture<HisApiJwtBearerEnforcedFactory>
{
    private readonly HttpClient _client;

    public HisJwtAuthorizationIntegrationTests(HisApiJwtBearerEnforcedFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Missing_permission_returns_403_with_json_body()
    {
        var jwt = HisApiJwtBearerEnforcedFactory.CreateBearerToken(new Claim(JwtRegisteredClaimNames.Sub, "limited-user"));
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/v1.0/operations/billing/export-jobs", UriKind.Relative));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        req.Content = JsonContent.Create(new { formatCode = "FHIR_BUNDLE_STUB" });
        using var response = await _client.SendAsync(req);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(403, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(HisPermissions.BillingExport, doc.RootElement.GetProperty("permission").GetString());
    }

    [Fact]
    public async Task With_billing_permission_billing_export_succeeds()
    {
        var jwt = HisApiJwtBearerEnforcedFactory.CreateBearerToken(
            new Claim(JwtRegisteredClaimNames.Sub, "billing-user"),
            new Claim("his_permission", HisPermissions.BillingExport));
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/v1.0/operations/billing/export-jobs", UriKind.Relative));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        req.Content = JsonContent.Create(new { formatCode = "FHIR_BUNDLE_STUB" });
        using var response = await _client.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }
}
