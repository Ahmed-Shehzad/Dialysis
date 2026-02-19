using BuildingBlocks.Tenancy;

using Dialysis.Fhir.Api;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Shouldly;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Fhir.Tests;

/// <summary>
/// Integration-style tests for FhirBulkExportService using a mock HttpClient.
/// </summary>
public sealed class FhirBulkExportServiceIntegrationTests
{
    [Fact]
    public async Task ExportAsync_WithMockedPatientResponse_MergesPatientIntoBundleAsync()
    {
        string patientJson = """
            {"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1","identifier":[{"value":"MRN001"}]}}]}
            """;
        var handler = new MockHttpMessageHandler(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/patients/fhir?limit=100"] = patientJson,
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");

        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(client, tenant);

        Bundle bundle = await service.ExportAsync(["Patient"], 100, null, null, null);

        bundle.Type.ShouldBe(Bundle.BundleType.Collection);
        bundle.Entry.ShouldNotBeEmpty();
        Patient? p = bundle.Entry.FirstOrDefault(e => e.Resource is Patient)?.Resource as Patient;
        p.ShouldNotBeNull();
        p.Id.ShouldBe("p1");
    }

    [Fact]
    public async Task ExportAsync_WithMultipleTypes_FetchesAndMergesFromMultiplePathsAsync()
    {
        string patientJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1"}}]}""";
        string deviceJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Device","id":"d1"}}]}""";
        var handler = new MockHttpMessageHandler(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/patients/fhir?limit=50"] = patientJson,
            ["api/devices/fhir?limit=50"] = deviceJson,
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");

        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(client, tenant);

        Bundle bundle = await service.ExportAsync(["Patient", "Device"], 50, null, null, null);

        bundle.Entry.Count.ShouldBe(2);
        bundle.Entry.Select(e => e.Resource?.TypeName).ShouldContain("Patient");
        bundle.Entry.Select(e => e.Resource?.TypeName).ShouldContain("Device");
    }

    [Fact]
    public async Task ExportAsync_WhenTreatmentSessionsReturnsProcedureAndObservation_MergesBothAsync()
    {
        string treatmentJson = """
            {"resourceType":"Bundle","type":"collection","entry":[
                {"resource":{"resourceType":"Procedure","id":"proc-sess1","status":"completed","subject":{"reference":"Patient/p1"}}},
                {"resource":{"resourceType":"Observation","id":"obs1","status":"final","code":{"coding":[{"system":"urn:iso:std:iso:11073:10101","code":"150456","display":"Blood pump flow"}]}}}
            ]}
            """;
        var handler = new MockHttpMessageHandler(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/treatment-sessions/fhir?limit=100"] = treatmentJson,
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");

        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(client, tenant);

        Bundle bundle = await service.ExportAsync(["Procedure", "Observation"], 100, null, null, null);

        bundle.Entry.Count.ShouldBe(2);
        bundle.Entry.Any(e => e.Resource is Procedure).ShouldBeTrue();
        bundle.Entry.Any(e => e.Resource is Observation).ShouldBeTrue();
    }

    [Fact]
    public async Task ExportAsync_WhenRequestedTypesEmpty_DefaultsToPatientAsync()
    {
        string patientJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1"}}]}""";
        var handler = new MockHttpMessageHandler(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/patients/fhir?limit=1000"] = patientJson,
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");

        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(client, tenant);

        Bundle bundle = await service.ExportAsync([], 1000, null, null, null);

        bundle.Entry.Count.ShouldBe(1);
        bundle.Entry[0].Resource.ShouldBeOfType<Patient>();
    }

    [Fact]
    public async Task ExportAsync_WhenPatientSpecified_ForwardsPatientToBackendsAsync()
    {
        string patientJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1","identifier":[{"value":"MRN001"}]}}]}""";
        string treatmentJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Procedure","id":"proc-1","status":"completed","subject":{"reference":"Patient/MRN001"}}}]}""";
        var handler = new MockHttpMessageHandler(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/patients/fhir?limit=100&identifier=MRN001"] = patientJson,
            ["api/treatment-sessions/fhir?limit=100&subject=MRN001&patient=MRN001"] = treatmentJson,
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");

        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(client, tenant);

        Bundle bundle = await service.ExportAsync(
            ["Patient", "Procedure"], 100, "MRN001", null, null);

        bundle.Entry.Count.ShouldBe(2);
        bundle.Entry.Any(e => e.Resource is Patient).ShouldBeTrue();
        bundle.Entry.Any(e => e.Resource is Procedure).ShouldBeTrue();
    }

    [Fact]
    public async Task ExportAsync_WhenBackendReturns404_OmitsThatTypeFromBundleAsync()
    {
        var handler = new MockHttpMessageHandler(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/patients/fhir?limit=100"] = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1"}}]}""",
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");

        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(client, tenant);

        Bundle bundle = await service.ExportAsync(["Patient", "Device"], 100, null, null, null);

        bundle.Entry.Count.ShouldBe(1);
        bundle.Entry[0].Resource.ShouldBeOfType<Patient>();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly IReadOnlyDictionary<string, string> _responses;

        public MockHttpMessageHandler(IReadOnlyDictionary<string, string> responses) => _responses = responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = (request.RequestUri?.PathAndQuery ?? "").TrimStart('/');
            if (_responses.TryGetValue(path, out string? json))
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/fhir+json"),
                });
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }

}
