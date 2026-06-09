using System.Net;
using System.Net.Http.Json;
using Dialysis.SmartConnect.Alerts;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AlertEndpointTests : IClassFixture<SmartConnectApiFactory>
{
    private readonly SmartConnectApiFactory _factory;

    public AlertEndpointTests(SmartConnectApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_Then_Get_Round_Trips_A_Rule_Async()
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

        var post = await client.PostAsJsonAsync("/api/v1/admin/alert-rules", payload);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await client.GetAsync($"/api/v1/admin/alert-rules/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = await get.Content.ReadAsStringAsync();
        Assert.Contains("ApiTestRule", body);
    }

    [Fact]
    public async Task Delete_Returns_204_And_Get_Returns_404_Async()
    {
        using var client = _factory.CreateClient();
        var id = Guid.CreateVersion7();
        await client.PostAsJsonAsync("/api/v1/admin/alert-rules", new
        {
            id,
            name = "ToDelete",
            enabled = true,
        });

        var del = await client.DeleteAsync($"/api/v1/admin/alert-rules/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/api/v1/admin/alert-rules/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task List_Events_Endpoint_Returns_Array_Async()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/admin/alert-events?take=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.StartsWith("[", body.TrimStart());
    }
}
