using System.Net;
using System.Net.Http;

using BuildingBlocks.Tenancy;

using Dialysis.Fhir.Api;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Shouldly;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Fhir.Tests;

/// <summary>
/// Integration-style tests for FhirBulkExportService using a mock IFhirExportGatewayApi.
/// </summary>
public sealed class FhirBulkExportServiceIntegrationTests
{
    [Fact]
    public async Task ExportAsync_WithMockedPatientResponse_MergesPatientIntoBundleAsync()
    {
        string patientJson = """
            {"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1","identifier":[{"value":"MRN001"}]}}]}
            """;
        var api = new MockFhirExportGatewayApi().Patients(patientJson);
        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(api, tenant);

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
        var api = new MockFhirExportGatewayApi().Patients(patientJson).Devices(deviceJson);
        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(api, tenant);

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
        var api = new MockFhirExportGatewayApi().TreatmentSessions(treatmentJson);
        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(api, tenant);

        Bundle bundle = await service.ExportAsync(["Procedure", "Observation"], 100, null, null, null);

        bundle.Entry.Count.ShouldBe(2);
        bundle.Entry.Any(e => e.Resource is Procedure).ShouldBeTrue();
        bundle.Entry.Any(e => e.Resource is Observation).ShouldBeTrue();
    }

    [Fact]
    public async Task ExportAsync_WhenRequestedTypesEmpty_DefaultsToPatientAsync()
    {
        string patientJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1"}}]}""";
        var api = new MockFhirExportGatewayApi().Patients(patientJson);
        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(api, tenant);

        Bundle bundle = await service.ExportAsync([], 1000, null, null, null);

        bundle.Entry.Count.ShouldBe(1);
        bundle.Entry[0].Resource.ShouldBeOfType<Patient>();
    }

    [Fact]
    public async Task ExportAsync_WhenPatientSpecified_ForwardsPatientToBackendsAsync()
    {
        string patientJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1","identifier":[{"value":"MRN001"}]}}]}""";
        string treatmentJson = """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Procedure","id":"proc-1","status":"completed","subject":{"reference":"Patient/MRN001"}}}]}""";
        var api = new MockFhirExportGatewayApi().Patients(patientJson).TreatmentSessions(treatmentJson);
        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(api, tenant);

        Bundle bundle = await service.ExportAsync(
            ["Patient", "Procedure"], 100, "MRN001", null, null);

        bundle.Entry.Count.ShouldBe(2);
        bundle.Entry.Any(e => e.Resource is Patient).ShouldBeTrue();
        bundle.Entry.Any(e => e.Resource is Procedure).ShouldBeTrue();
    }

    [Fact]
    public async Task ExportAsync_WhenBackendReturns404_OmitsThatTypeFromBundleAsync()
    {
        var api = new MockFhirExportGatewayApi()
            .Patients("""{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Patient","id":"p1"}}]}""")
            .Devices(status: HttpStatusCode.NotFound);
        var tenant = new TenantContext { TenantId = "default" };
        var service = new FhirBulkExportService(api, tenant);

        Bundle bundle = await service.ExportAsync(["Patient", "Device"], 100, null, null, null);

        bundle.Entry.Count.ShouldBe(1);
        bundle.Entry[0].Resource.ShouldBeOfType<Patient>();
    }

#pragma warning disable IDE0044
    private sealed class MockFhirExportGatewayApi : IFhirExportGatewayApi
    {
        private string _patients = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _patientsStatus = HttpStatusCode.OK;
        private string _devices = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _devicesStatus = HttpStatusCode.OK;
        private string _prescriptions = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _prescriptionsStatus = HttpStatusCode.OK;
        private string _treatmentSessions = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _treatmentSessionsStatus = HttpStatusCode.OK;
        private string _alarms = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _alarmsStatus = HttpStatusCode.OK;
        private string _auditEvents = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _auditEventsStatus = HttpStatusCode.OK;
#pragma warning restore IDE0044

        public MockFhirExportGatewayApi Patients(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _patients = body;
            _patientsStatus = status;
            return this;
        }

        public MockFhirExportGatewayApi Devices(string body = """{"resourceType":"Bundle","type":"collection","entry":[]}""", HttpStatusCode status = HttpStatusCode.OK)
        {
            _devices = body;
            _devicesStatus = status;
            return this;
        }

        public MockFhirExportGatewayApi TreatmentSessions(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _treatmentSessions = body;
            _treatmentSessionsStatus = status;
            return this;
        }

        public Task<HttpResponseMessage> GetPatientsFhirAsync(int limit, string? identifier, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response(_patients, _patientsStatus));

        public Task<HttpResponseMessage> GetDevicesFhirAsync(int limit, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response(_devices, _devicesStatus));

        public Task<HttpResponseMessage> GetPrescriptionsFhirAsync(int limit, string? subject, string? patient, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response(_prescriptions, _prescriptionsStatus));

        public Task<HttpResponseMessage> GetTreatmentSessionsFhirAsync(int limit, string? subject, string? patient, string? dateFrom, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response(_treatmentSessions, _treatmentSessionsStatus));

        public Task<HttpResponseMessage> GetAlarmsFhirAsync(int limit, string? from, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response(_alarms, _alarmsStatus));

        public Task<HttpResponseMessage> GetAuditEventsAsync(int count, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response(_auditEvents, _auditEventsStatus));

        private static HttpResponseMessage Response(string json, HttpStatusCode status) =>
            new(status)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/fhir+json"),
            };
    }
}
