using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Dialysis.HIE.Query;
using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Tefca.Ias;
using Dialysis.HIE.Tefca.Ports;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Query;

public sealed class PartnerFhirQueryClientTests
{
    private const string SigningKey = "test-signing-key-at-least-32-bytes-long-12345";
    private const string SearchsetBundle =
        """{"resourceType":"Bundle","type":"searchset","entry":[{"resource":{"resourceType":"Patient","id":"p1"}},{"resource":{"resourceType":"Observation","id":"o1","status":"final"}}]}""";

    [Fact]
    public async Task Pull_Returns_Resources_And_Sends_A_Purpose_Scoped_Ias_Jwt_Async()
    {
        var partner = ActivePartner();
        var handler = new StubHandler(SearchsetBundle);
        var client = MakeClient(handler, partner);

        var resources = await client.QueryAsync(partner.Id, "Patient/p1/$everything", "p1", "Treatment", CancellationToken.None);

        resources.Count.ShouldBe(2);
        resources.ShouldContain(r => r is Patient);
        resources.ShouldContain(r => r is Observation);

        handler.Authorization.ShouldNotBeNull();
        handler.Authorization!.Scheme.ShouldBe("Bearer");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(handler.Authorization.Parameter);
        jwt.Claims.ShouldContain(c => c.Type == "purpose_of_use" && c.Value == "Treatment");
        jwt.Claims.ShouldContain(c => c.Type == "sub" && c.Value == "p1");
        jwt.Audiences.ShouldContain("https://qhin.example/ias");
    }

    [Fact]
    public async Task Rejects_A_Purpose_The_Partner_Does_Not_Permit_Async()
    {
        var partner = ActivePartner();
        partner.SetAllowedPurposes(["Payment"], DateTime.UtcNow, "dpo");
        var client = MakeClient(new StubHandler(SearchsetBundle), partner);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            client.QueryAsync(partner.Id, "Patient", "p1", "Treatment", CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_A_Non_Active_Partner_Async()
    {
        var partner = new QhinPartner(
            Guid.CreateVersion7(), "Acme QHIN", "https://qhin.example/fhir", "https://qhin.example/ias",
            DateTime.UtcNow, "dpo"); // stays Onboarding
        var client = MakeClient(new StubHandler(SearchsetBundle), partner);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            client.QueryAsync(partner.Id, "Patient", "p1", "Treatment", CancellationToken.None));
    }

    private static PartnerFhirQueryClient MakeClient(StubHandler handler, QhinPartner partner) => new(
        new StubHttpClientFactory(handler),
        new HmacIasJwtIssuer(Options.Create(new IasJwtIssuerOptions { SigningKey = SigningKey }), TimeProvider.System),
        new StubPartnerRepo(partner),
        Options.Create(new PartnerFhirQueryOptions()),
        NullLogger<PartnerFhirQueryClient>.Instance);

    private static QhinPartner ActivePartner()
    {
        var partner = new QhinPartner(
            Guid.CreateVersion7(), "Acme QHIN", "https://qhin.example/fhir", "https://qhin.example/ias",
            DateTime.UtcNow, "dpo");
        partner.AttachTrustAnchor(new QhinTrustAnchor(
            Guid.CreateVersion7(), partner.Id, "CN=Acme", "AA",
            "-----BEGIN CERTIFICATE-----stub-----END CERTIFICATE-----",
            DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(5), DateTime.UtcNow, "dpo"));
        partner.RotateMtls("inmem://x", "MTLS-AA", DateTime.UtcNow, "dpo");
        partner.TransitionStatus(QhinPartnerStatus.Active, DateTime.UtcNow, "dpo");
        return partner;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        public StubHandler(string body) => _body = body;
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/fhir+json"),
            });
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler);
    }

    private sealed class StubPartnerRepo : IQhinPartnerRepository
    {
        private readonly QhinPartner _partner;
        public StubPartnerRepo(QhinPartner partner) => _partner = partner;
        public void Add(QhinPartner partner) { }
        public void Remove(QhinPartner partner) { }
        public Task<QhinPartner?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<QhinPartner?>(_partner.Id == id ? _partner : null);
        public Task<IReadOnlyList<QhinPartner>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QhinPartner>>([_partner]);
    }
}
