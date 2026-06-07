using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Slice B2: the operator-shell relies on these endpoints to render a form-driven editor
/// per outbound adapter kind, so the contract is: GET /connectors/outbound lists every
/// registered adapter with a <c>hasSchema</c> flag, and GET /connectors/outbound/{kind}/schema
/// returns the JSON Schema for adapters that have published one.
/// </summary>
public sealed class ConnectorSchemaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConnectorSchemaEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task List_Endpoint_Returns_Every_Registered_Outbound_Adapter_Kind_Async()
    {
        using var client = _factory.CreateClient();

        var entries = await client.GetFromJsonAsync<List<Entry>>("/api/v1/admin/connectors/outbound");

        Assert.NotNull(entries);
        // Built-in kinds registered by AddSmartConnectCore.
        var kinds = entries!.Select(e => e.Kind).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("http", kinds);
        Assert.Contains("file", kinds);
        Assert.Contains("tcp", kinds);
        Assert.Contains("channel-writer", kinds);
    }

    [Fact]
    public async Task List_Endpoint_Flags_Adapters_With_Published_Schema_Async()
    {
        using var client = _factory.CreateClient();
        var entries = await client.GetFromJsonAsync<List<Entry>>("/api/v1/admin/connectors/outbound");

        var http = Assert.Single(entries!, e => e.Kind == "http");
        Assert.True(http.HasSchema);
    }

    [Fact]
    public async Task Schema_Endpoint_Returns_Json_Schema_For_Http_Adapter_Async()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/connectors/outbound/http/schema");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/schema+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("HttpOutboundParameters", doc.RootElement.GetProperty("title").GetString());
        Assert.True(doc.RootElement.GetProperty("properties").TryGetProperty("Url", out _));
        Assert.True(doc.RootElement.GetProperty("properties").TryGetProperty("Authentication", out _));
        Assert.True(doc.RootElement.GetProperty("properties").TryGetProperty("ConnectorProperties", out _));
    }

    [Fact]
    public async Task Schema_Endpoint_Returns_404_For_Unknown_Adapter_Async()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/connectors/outbound/no-such-adapter/schema");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Schema_Endpoint_Returns_404_When_Adapter_Has_No_Schema_Async()
    {
        using var client = _factory.CreateClient();

        // The TCP outbound adapter ships without a schema yet (slice B2 only published
        // HTTP). The endpoint distinguishes "no adapter" from "no schema" via 404 body.
        var response = await client.GetAsync("/api/v1/admin/connectors/outbound/tcp/schema");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // [JsonPropertyName] binds the camelCase JSON property names to the PascalCase
    // backing fields the test-naming convention requires.
    private sealed class Entry
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "";

        [JsonPropertyName("hasSchema")]
        public bool HasSchema { get; set; }
    }
}
