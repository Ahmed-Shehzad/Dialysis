using System.Net;
using System.Text;
using Dialysis.BuildingBlocks.Fhir.Terminology;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Terminology;

public sealed class HttpFhirTerminologyServiceTests
{
    [Fact]
    public async Task Lookup_Calls_Codesystem_Lookup_Endpoint_Async()
    {
        var stub = new StubHandler((req, _) =>
        {
            req.RequestUri!.AbsoluteUri.ShouldContain("/CodeSystem/$lookup");
            req.RequestUri.Query.ShouldContain("system=http%3A%2F%2Floinc.org");
            req.RequestUri.Query.ShouldContain("code=11506-3");
            return Json_Response("""
            {"resourceType":"Parameters","parameter":[
              {"name":"name","valueString":"LOINC"},
              {"name":"display","valueString":"Subsequent evaluation note"}
            ]}
            """);
        });

        var sut = Make_Sut(stub);
        var result = await sut.LookupAsync("http://loinc.org", "11506-3", CancellationToken.None);

        result.Parameter.ShouldContain(p => p.Name == "display");
    }

    [Fact]
    public async Task Validatecode_Builds_Url_With_Code_And_System_Async()
    {
        var stub = new StubHandler((req, _) =>
        {
            req.RequestUri!.AbsoluteUri.ShouldContain("/ValueSet/$validate-code");
            req.RequestUri.Query.ShouldContain("code=265764009");
            return Json_Response("""{"resourceType":"Parameters","parameter":[{"name":"result","valueBoolean":true}]}""");
        });

        var sut = Make_Sut(stub);
        var result = await sut.ValidateCodeAsync("http://example.org/vs", "265764009", "http://snomed.info/sct", CancellationToken.None);

        var resultParam = result.Parameter.FirstOrDefault(p => p.Name == "result");
        resultParam.ShouldNotBeNull();
    }

    [Fact]
    public async Task Failed_Upstream_Returns_Empty_Parameters_Async()
    {
        var stub = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = Make_Sut(stub);
        var result = await sut.LookupAsync("http://loinc.org", "missing", CancellationToken.None);

        result.Parameter.ShouldBeEmpty();
    }

    private static HttpFhirTerminologyService Make_Sut(StubHandler stub)
    {
        var http = new HttpClient(stub) { BaseAddress = new Uri("https://tx.test/r4/") };
        var options = Options.Create(new FhirTerminologyOptions { Endpoint = "https://tx.test/r4" });
        return new HttpFhirTerminologyService(http, options, NullLogger<HttpFhirTerminologyService>.Instance);
    }

    private static HttpResponseMessage Json_Response(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/fhir+json"),
    };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request, cancellationToken));
    }
}
