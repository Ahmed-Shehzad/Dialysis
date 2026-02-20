using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using Shouldly;

using Xunit;

namespace Dialysis.Gateway.Tests;

/// <summary>
/// Integration tests for Gateway health aggregation.
/// </summary>
public sealed class GatewayHealthTests : IClassFixture<WebApplicationFactory<Dialysis.Gateway.Program>>
{
    private readonly HttpClient _client;

    public GatewayHealthTests(WebApplicationFactory<Dialysis.Gateway.Program> factory)
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
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("status", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldNotBeNullOrEmpty();

        root.TryGetProperty("entries", out JsonElement entries).ShouldBeTrue();
        entries.ValueKind.ShouldBe(JsonValueKind.Object);
    }
}
