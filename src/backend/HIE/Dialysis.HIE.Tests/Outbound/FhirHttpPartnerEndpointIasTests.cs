using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Outbound.Partners.Http;
using Dialysis.HIE.Tefca.Ias;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class FhirHttpPartnerEndpointIasTests
{
    private const string SigningKey = "test-signing-key-at-least-32-bytes-long-12345";

    [Fact]
    public async Task Outbound_Call_Carries_A_Patient_And_Purpose_Scoped_Ias_Jwt_Async()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        var options = new PartnerHttpOptions
        {
            BaseUrl = "https://partner.example/fhir/",
            UseIasJwt = true,
            IasAudience = "https://partner.example/ias",
            IasIssuer = "DialysisPlatform.Tefca",
            IasScope = "patient.exchange",
        };
        var issuer = new HmacIasJwtIssuer(
            Options.Create(new IasJwtIssuerOptions { SigningKey = SigningKey }), TimeProvider.System);
        var endpoint = new FhirHttpPartnerEndpoint(
            "default", http, options, NullLogger<FhirHttpPartnerEndpoint>.Instance, issuer);

        var patientId = Guid.NewGuid();
        var result = await endpoint.DeliverAsync(
            new Patient { Id = "p1" }, new PartnerDeliveryContext(patientId, "Treatment"), CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        handler.Authorization.ShouldNotBeNull();
        handler.Authorization!.Scheme.ShouldBe("Bearer");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(handler.Authorization.Parameter);
        jwt.Claims.ShouldContain(c => c.Type == "purpose_of_use" && c.Value == "Treatment");
        jwt.Claims.ShouldContain(c => c.Type == "sub" && c.Value == patientId.ToString());
        jwt.Claims.ShouldContain(c => c.Type == "scope" && c.Value == "patient.exchange");
        jwt.Audiences.ShouldContain("https://partner.example/ias");
    }

    [Fact]
    public async Task Falls_Back_To_Static_Token_When_Ias_Disabled_Async()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        var options = new PartnerHttpOptions
        {
            BaseUrl = "https://partner.example/fhir/",
            UseIasJwt = false,
            BearerToken = "static-token",
        };
        var endpoint = new FhirHttpPartnerEndpoint(
            "default", http, options, NullLogger<FhirHttpPartnerEndpoint>.Instance);

        await endpoint.DeliverAsync(
            new Patient { Id = "p1" }, new PartnerDeliveryContext(Guid.NewGuid(), "Treatment"), CancellationToken.None);

        handler.Authorization!.Parameter.ShouldBe("static-token");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
