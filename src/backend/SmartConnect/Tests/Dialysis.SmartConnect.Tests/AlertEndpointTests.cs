using System.Net;
using System.Net.Http.Json;
using Dialysis.SmartConnect.Alerts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AlertEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlertEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Post_then_get_round_trips_a_rule()
    {
        using var client = _factory.CreateClient();
        var id = Guid.CreateVersion7();
        var payload = new
        {
            id,
            name = "ApiTestRule",
            enabled = true,
            errorPatterns = new[]
            {
                new { errorType = AlertErrorType.OutboundFailure, regex = (string?)null },
            },
            actions = new[]
            {
                new { kind = "webhook", propertiesJson = """{"url":"https://x"}""" },
            },
        };

        var post = await client.PostAsJsonAsync("/smartconnect/v1/admin/alert-rules", payload);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await client.GetAsync($"/smartconnect/v1/admin/alert-rules/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = await get.Content.ReadAsStringAsync();
        Assert.Contains("ApiTestRule", body);
    }

    [Fact]
    public async Task Delete_returns_204_and_get_returns_404()
    {
        using var client = _factory.CreateClient();
        var id = Guid.CreateVersion7();
        await client.PostAsJsonAsync("/smartconnect/v1/admin/alert-rules", new
        {
            id,
            name = "ToDelete",
            enabled = true,
        });

        var del = await client.DeleteAsync($"/smartconnect/v1/admin/alert-rules/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/smartconnect/v1/admin/alert-rules/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task List_events_endpoint_returns_array()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/smartconnect/v1/admin/alert-events?take=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.StartsWith("[", body.TrimStart());
    }
}
