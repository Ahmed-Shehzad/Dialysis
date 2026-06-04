using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Dialysis.HIE.Tefca.Ias;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Tefca;

public sealed class HmacIasJwtIssuerTests
{
    private const string SigningKey = "test-signing-key-at-least-32-bytes-long-12345";

    [Fact]
    public void Issued_Token_Validates_Round_Trip()
    {
        var sut = Make_Issuer();
        var token = sut.Issue(new IasJwtRequest(
            Issuer: "DialysisPlatform.Tefca",
            Audience: "https://qhin.example/ias",
            Subject: Guid.NewGuid().ToString("N"),
            Scope: "patient.read",
            Lifetime: TimeSpan.FromMinutes(5)));

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "DialysisPlatform.Tefca",
            ValidateAudience = true,
            ValidAudience = "https://qhin.example/ias",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ClockSkew = TimeSpan.Zero,
        };
        handler.ValidateToken(token, parameters, out var validated);
        var jwt = (JwtSecurityToken)validated;
        jwt.Claims.ShouldContain(c => c.Type == "scope" && c.Value == "patient.read");
        jwt.Claims.ShouldContain(c => c.Type == "tefca_role" && c.Value == "qhin");
        jwt.Claims.ShouldContain(c => c.Type == "originator" && c.Value == "DialysisPlatform.Tefca");
    }

    [Fact]
    public void Missing_Signing_Key_Throws_With_Clear_Message()
    {
        var sut = new HmacIasJwtIssuer(
            Options.Create(new IasJwtIssuerOptions { SigningKey = null }),
            TimeProvider.System);
        var ex = Should.Throw<InvalidOperationException>(() => sut.Issue(new IasJwtRequest(
            "iss", "aud", "sub", "patient.read", TimeSpan.FromMinutes(5))));
        ex.Message.ShouldContain("Tefca:IasJwtIssuer:SigningKey");
    }

    [Fact]
    public void Non_Positive_Lifetime_Throws()
    {
        var sut = Make_Issuer();
        Should.Throw<ArgumentException>(() => sut.Issue(new IasJwtRequest(
            "iss", "aud", "sub", "patient.read", TimeSpan.Zero)));
    }

    private static HmacIasJwtIssuer Make_Issuer() => new(
        Options.Create(new IasJwtIssuerOptions { SigningKey = SigningKey }),
        TimeProvider.System);
}
