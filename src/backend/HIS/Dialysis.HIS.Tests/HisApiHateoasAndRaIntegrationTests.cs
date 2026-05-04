using System.Net.Http.Json;
using System.Text.Json;
using Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;
using Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;
using Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;
using Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;
using Asp.Versioning;
using Dialysis.HIS.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Tests;

/// <summary>Each factory instance gets a unique EF in-memory database name so parallel xUnit collections do not collide on seed keys.</summary>
public abstract class HisApiWebApplicationFactoryBase : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDbName = "HisApiTest_" + Guid.NewGuid().ToString("n");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("His:InMemoryDatabaseName", _inMemoryDbName);
        ConfigureHisTestWebHost(builder);
    }

    protected virtual void ConfigureHisTestWebHost(IWebHostBuilder builder)
    {
    }
}

public sealed class HisApiDefaultFactory : HisApiWebApplicationFactoryBase;

/// <summary>Overrides default API version so neutral <c>/health</c> links can be regression-tested without duplicating version literals.</summary>
public sealed class HisApiHealthLinksVersionOverrideFactory : HisApiWebApplicationFactoryBase
{
    protected override void ConfigureHisTestWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(static services =>
        {
            services.PostConfigure<ApiVersioningOptions>(o => o.DefaultApiVersion = new ApiVersion(2, 0));
        });
    }
}

public sealed class HisApiHateoasEnvelopeTests : IClassFixture<HisApiDefaultFactory>
{
    private readonly HttpClient _client;

    public HisApiHateoasEnvelopeTests(HisApiDefaultFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_resource_envelope_with_hateoas_links()
    {
        using var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal("healthy", data.GetProperty("status").GetString());
        Assert.Equal("HIS", data.GetProperty("module").GetString());
        Assert.True(root.TryGetProperty("links", out var links));
        Assert.NotEqual(JsonValueKind.Undefined, links.ValueKind);
        var linkList = links.EnumerateArray().ToList();
        Assert.Contains(linkList, l => l.GetProperty("rel").GetString() == "self");
        var catalog = linkList.Single(l => l.GetProperty("rel").GetString() == "ra:catalog");
        Assert.Equal("GET", catalog.GetProperty("method").GetString());
        Assert.Contains("/api/v1.0/reference-architecture/catalog", catalog.GetProperty("href").GetString(), StringComparison.Ordinal);
        var cap = linkList.Single(l => l.GetProperty("rel").GetString() == "ra:capabilities-index");
        Assert.Contains("/api/v1.0/reference-architecture/capabilities", cap.GetProperty("href").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_returns_envelope_with_documentation_paths_and_openapi_link()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1.0/help", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal("HIS API discovery", data.GetProperty("title").GetString());
        Assert.True(data.TryGetProperty("documentation", out var docs));
        Assert.True(docs.GetArrayLength() >= 3);
        Assert.Contains(
            docs.EnumerateArray(),
            d => d.GetProperty("repositoryRelativePath").GetString()?.Contains("his_ra_submodules.md", StringComparison.Ordinal) == true);
        Assert.True(root.TryGetProperty("links", out var links));
        var linkList = links.EnumerateArray().ToList();
        Assert.Contains(linkList, l => l.GetProperty("rel").GetString() == "openapi");
        Assert.Contains(linkList, l => l.GetProperty("rel").GetString() == "ra:capabilities-index");
    }

    [Fact]
    public async Task Reference_architecture_catalog_returns_envelope_with_links()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1.0/reference-architecture/catalog", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("modules", out var modules));
        Assert.Equal(JsonValueKind.Array, modules.ValueKind);
        Assert.True(root.TryGetProperty("links", out var links));
        Assert.Contains(links.EnumerateArray(), l => l.GetProperty("rel").GetString() == "self");
    }
}

public sealed class HisApiHealthDefaultVersionLinkTests : IClassFixture<HisApiHealthLinksVersionOverrideFactory>
{
    private readonly HttpClient _client;

    public HisApiHealthDefaultVersionLinkTests(HisApiHealthLinksVersionOverrideFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Health_hateoas_links_use_configured_default_api_version_segment()
    {
        using var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var links = doc.RootElement.GetProperty("links").EnumerateArray().ToList();
        var catalog = links.Single(l => l.GetProperty("rel").GetString() == "ra:catalog");
        Assert.Contains("/api/v2.0/reference-architecture/catalog", catalog.GetProperty("href").GetString(), StringComparison.Ordinal);
        var cap = links.Single(l => l.GetProperty("rel").GetString() == "ra:capabilities-index");
        Assert.Contains("/api/v2.0/reference-architecture/capabilities", cap.GetProperty("href").GetString(), StringComparison.Ordinal);
    }
}

public sealed class HisApiRaCapabilitiesIntegrationTests : IClassFixture<HisApiDefaultFactory>
{
    private readonly HttpClient _client;

    public HisApiRaCapabilitiesIntegrationTests(HisApiDefaultFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Capabilities_index_returns_envelope_with_endpoint_links()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1.0/reference-architecture/capabilities", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("endpoints", out var endpoints));
        Assert.Equal(JsonValueKind.Array, endpoints.ValueKind);
        Assert.Equal(11, endpoints.GetArrayLength());
        Assert.All(
            endpoints.EnumerateArray(),
            e =>
            {
                Assert.True(e.TryGetProperty("rel", out _));
                Assert.True(e.TryGetProperty("href", out _));
                Assert.True(e.TryGetProperty("method", out _));
            });
        var links = root.GetProperty("links").EnumerateArray().ToList();
        Assert.Contains(links, l => l.GetProperty("rel").GetString() == "self");
    }

    [Fact]
    public async Task Ra_org_communications_returns_envelope_and_rows_from_seed()
    {
        using var response = await _client.GetAsync(
            new Uri("/api/v1.0/reference-architecture/capabilities/generic-mis/organizational-communications", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.NotEmpty(data.EnumerateArray());
        Assert.True(root.TryGetProperty("links", out var links));
        Assert.Contains(links.EnumerateArray(), l => l.GetProperty("rel").GetString() == "self");
    }

    [Fact]
    public async Task Full_text_capabilities_supports_optional_q_filter()
    {
        using var response = await _client.GetAsync(
            new Uri("/api/v1.0/reference-architecture/capabilities/data-management/full-text-and-indexing?q=demo", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }

    [Fact]
    public async Task Waitlist_enqueue_returns_created()
    {
        var demoPatientId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0c");
        var body = new EnqueueWaitlistEntryCommand(
            demoPatientId,
            ResourceKindCode: "dialysis-room",
            Notes: "integration-test",
            RequestedNotBeforeUtc: DateTime.UtcNow.Date);
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1.0/reference-architecture/capabilities/planning-and-scheduling/waitlists", UriKind.Relative),
            body);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
}

public sealed class HisApiDataShareBillingAndRaWritesIntegrationTests : IClassFixture<HisApiDefaultFactory>
{
    private readonly HttpClient _client;

    public HisApiDataShareBillingAndRaWritesIntegrationTests(HisApiDefaultFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Integration_outbox_metadata_returns_envelope()
    {
        using var response = await _client.GetAsync(
            new Uri("/api/v1.0/data-management/integration/outbox-metadata?take=10", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }

    [Fact]
    public async Task Billing_export_job_roundtrip_get_by_id()
    {
        var create = await _client.PostAsJsonAsync(
            new Uri("/api/v1.0/operations/billing/export-jobs", UriKind.Relative),
            new { formatCode = "FHIR_BUNDLE_STUB", payerCode = "PAYER_DEMO" });
        create.EnsureSuccessStatusCode();
        using var createdDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = createdDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        using var get = await _client.GetAsync(new Uri($"/api/v1.0/operations/billing/export-jobs/{id}", UriKind.Relative));
        get.EnsureSuccessStatusCode();
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var row = getDoc.RootElement.GetProperty("data");
        Assert.Equal("Queued", row.GetProperty("statusCode").GetString());
        Assert.Equal("PAYER_DEMO", row.GetProperty("payerCode").GetString());
    }

    [Fact]
    public async Task Manager_dashboard_echoes_report_focus_and_extra_metrics()
    {
        using var response = await _client.GetAsync(
            new Uri("/api/v1.0/data-management/manager-dashboard?reportFocus=ops-weekly", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("ops-weekly", data.GetProperty("appliedReportFocus").GetString());
        Assert.True(data.TryGetProperty("queuedBillingExports", out _));
        Assert.True(data.TryGetProperty("openQualityWorkflowTasks", out _));
    }

    [Fact]
    public async Task Ra_ehr_quality_and_security_write_paths_succeed()
    {
        var demoPatientId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0c");
        var demoQualityTaskId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd02");

        using var ehr = await _client.PostAsJsonAsync(
            new Uri("/api/v1.0/reference-architecture/capabilities/patient-monitoring/ehr-document-exchange/records", UriKind.Relative),
            new RegisterEhrDocumentExchangeCommand(
                demoPatientId,
                DocumentTypeCode: "integration-note",
                ExternalSystemCode: "ehr-test",
                ExternalUri: "urn:his:test:doc:1"));
        Assert.Equal(System.Net.HttpStatusCode.Created, ehr.StatusCode);

        using var sec = await _client.PostAsJsonAsync(
            new Uri("/api/v1.0/reference-architecture/capabilities/security/mechanisms-hardening/assessments", UriKind.Relative),
            new RecordSecurityMechanismAssessmentCommand("tls-1.3", "enforced", "integration assessment row"));
        Assert.Equal(System.Net.HttpStatusCode.Created, sec.StatusCode);

        using var q = await _client.PostAsJsonAsync(
            new Uri($"/api/v1.0/reference-architecture/capabilities/generic-mis/quality-workflows/{demoQualityTaskId}/status", UriKind.Relative),
            new QualityTaskStatusBody("closed"));
        Assert.Equal(System.Net.HttpStatusCode.NoContent, q.StatusCode);
    }

    private sealed record QualityTaskStatusBody(string NewStatusCode);
}
