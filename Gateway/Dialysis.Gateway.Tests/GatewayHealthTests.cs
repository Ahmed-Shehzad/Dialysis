using System.Net;
using System.Text.Json;

using Shouldly;

using Xunit;

namespace Dialysis.Gateway.Tests;

/// <summary>
/// Integration tests for Gateway health aggregation.
/// Uses test-mode health checks (no backend URLs or NTP) so tests run reliably in CI.
/// </summary>
public sealed class GatewayHealthTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GatewayHealthTests(GatewayWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsJsonWithStatusAndEntriesAsync()
    {
        HttpResponseMessage response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("status", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldNotBeNullOrEmpty();

        root.TryGetProperty("entries", out JsonElement entries).ShouldBeTrue();
        entries.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    /// <summary>
    /// Verifies that /api/patients/* is routed (YARP matches the route).
    /// When backends are down, YARP returns 502; 404 would indicate no route matched.
    /// </summary>
    [Fact]
    public async Task PatientRoute_WhenBackendUnavailable_Returns502Async()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/patients/123");

        // 502 = route matched, backend unreachable; 404 = no route matched
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable);
    }
}
