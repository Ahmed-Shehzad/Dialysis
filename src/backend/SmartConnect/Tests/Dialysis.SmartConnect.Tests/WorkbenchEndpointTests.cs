using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers POST /api/v1/admin/workbench/{parse-hl7,validate-hl7,dispatch}.
/// All payloads are operator-supplied per the production-readiness rule (no canned data).
/// </summary>
public sealed class WorkbenchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WorkbenchEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ParseHl7_Returns_Header_And_Segment_Names_Async()
    {
        using var client = _factory.CreateClient();
        var payload = "MSH|^~\\&|SENDA|FACA|RECB|FACB|20260101010101||ORU^R01|CTRL1|P|2.5\r"
            + "PID|1||MRN-1||DOE^JANE||19800101|F\r"
            + "OBR|1|||GLU^Glucose\r"
            + "OBX|1|NM|GLU^Glucose||95|mg/dL||N";

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/workbench/parse-hl7",
            new { payloadText = payload });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("2.5", doc.RootElement.GetProperty("header").GetProperty("version").GetString());
        Assert.Equal("R01", doc.RootElement.GetProperty("header").GetProperty("trigger").GetString());
        var segments = doc.RootElement.GetProperty("segmentNames").EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();
        Assert.Contains("MSH", segments);
        Assert.Contains("PID", segments);
        Assert.Contains("OBX", segments);
    }

    [Fact]
    public async Task ParseHl7_Returns_422_For_Bad_Payload_Async()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/workbench/parse-hl7",
            new { payloadText = "NOT-HL7" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ValidateHl7_Returns_Pass_For_Min_Version_Met_Async()
    {
        using var client = _factory.CreateClient();
        var payload = "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|C|P|2.5\rPID|1||MRN";
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/workbench/validate-hl7",
            new { payloadText = payload, minVersion = "2.3", requiredSegments = new[] { "MSH", "PID" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("isValid").GetBoolean());
    }

    [Fact]
    public async Task ValidateHl7_Returns_Fail_When_Required_Segment_Missing_Async()
    {
        using var client = _factory.CreateClient();
        var payload = "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|C|P|2.5\rPID|1||MRN";
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/workbench/validate-hl7",
            new { payloadText = payload, requiredSegments = new[] { "MSH", "PID", "PV1" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Contains("PV1", doc.RootElement.GetProperty("reason").GetString() ?? "");
    }

    [Fact]
    public async Task Dispatch_Returns_404_For_Unknown_Flow_Async()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/workbench/dispatch",
            new
            {
                flowId = Guid.NewGuid(),
                payloadText = "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|C|P|2.5\rPID|1||MRN",
            });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dispatch_Runs_Payload_Through_Existing_Flow_Async()
    {
        using var client = _factory.CreateClient();
        var flowId = await CreateFlow_Async(client, "workbench-target");

        // The flow must be Started before DispatchAsync will route through it.
        var start = await client.PostAsync($"/api/v1/admin/flows/{flowId}/start", content: null);
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);

        var payload = "MSH|^~\\&|SENDA|FACA|RECB|FACB|20260101010101||ADT^A01|CTRL2|P|2.5\rPID|1||MRN";
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/workbench/dispatch",
            new { flowId, payloadText = payload });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("dispatchedMessageId", out _));
        Assert.True(doc.RootElement.TryGetProperty("correlationId", out var corrEl));
        Assert.StartsWith("workbench-", corrEl.GetString());
    }

    private static async Task<Guid> CreateFlow_Async(HttpClient client, string name)
    {
        var id = Guid.NewGuid();
        var body = new
        {
            id,
            name,
            runtimeState = 0,
            description = (string?)null,
            tags = Array.Empty<string>(),
            dataTypes = new[] { "HL7v2" },
            dependencies = Array.Empty<Guid>(),
            attachments = Array.Empty<object>(),
            pipeline = new
            {
                routeFilters = new[] { new { kind = "allow-all" } },
                sourceTransformStages = Array.Empty<object>(),
                outboundRoutesSequential = false,
                outboundRoutes = new[]
                {
                    new
                    {
                        outboundAdapterKind = "pass-through",
                        outboundParametersJson = (string?)null,
                    },
                },
                linkedLibraryIds = Array.Empty<string>(),
            },
        };
        var response = await client.PostAsJsonAsync("/api/v1/admin/flows", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Create '{name}' returned {response.StatusCode}: {text}");
        return id;
    }
}
