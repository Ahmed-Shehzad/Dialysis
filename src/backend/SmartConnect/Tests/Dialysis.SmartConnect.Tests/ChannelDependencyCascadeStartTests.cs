using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers POST /flows/{id}/start?cascade=true:
///  - Walks declared dependencies depth-first, Starting any that aren't already Started.
///  - 409 + cycle path when the dependency graph contains a cycle.
///  - Skips dependencies that are already Started.
/// </summary>
public sealed class ChannelDependencyCascadeStartTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChannelDependencyCascadeStartTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Cascade_Starts_Entire_Dependency_Chain_In_Order_Async()
    {
        using var client = _factory.CreateClient();
        // Graph: leaf → mid → root (root has no deps; leaf depends on mid; mid depends on root)
        var rootId = await CreateFlow_Async(client, "cascade-root");
        var midId = await CreateFlow_Async(client, "cascade-mid", dependencies: [rootId]);
        var leafId = await CreateFlow_Async(client, "cascade-leaf", dependencies: [midId]);

        var response = await client.PostAsync($"/smartconnect/v1/admin/flows/{leafId}/start?cascade=true", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(3, doc.RootElement.GetProperty("count").GetInt32());
        var startedIds = doc.RootElement.GetProperty("started")
            .EnumerateArray()
            .Select(e => Guid.Parse(e.GetProperty("id").GetString()!))
            .ToArray();
        // Root must be first, leaf last.
        Assert.Equal(rootId, startedIds[0]);
        Assert.Equal(leafId, startedIds[^1]);
    }

    [Fact]
    public async Task Cascade_Skips_Already_Started_Dependencies_Async()
    {
        using var client = _factory.CreateClient();
        var depId = await CreateFlow_Async(client, "already-up");
        var dependentId = await CreateFlow_Async(client, "needs-up", dependencies: [depId]);

        var preStart = await client.PostAsync($"/smartconnect/v1/admin/flows/{depId}/start", content: null);
        Assert.Equal(HttpStatusCode.NoContent, preStart.StatusCode);

        var response = await client.PostAsync($"/smartconnect/v1/admin/flows/{dependentId}/start?cascade=true", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        // Only the dependent itself should appear in the started list — the dep was already up.
        Assert.Equal(1, doc.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Cascade_Refuses_With_409_When_Graph_Has_Cycle_Async()
    {
        using var client = _factory.CreateClient();
        // Build a→b first (both with no deps), then update a to depend on b AND b to depend on a.
        var aId = await CreateFlow_Async(client, "cycle-a");
        var bId = await CreateFlow_Async(client, "cycle-b", dependencies: [aId]);
        await UpdateDependencies_Async(client, aId, "cycle-a", [bId]);

        var response = await client.PostAsync($"/smartconnect/v1/admin/flows/{aId}/start?cascade=true", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("cycle", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cyclePath", body);
    }

    private static async Task<Guid> CreateFlow_Async(
        HttpClient client,
        string name,
        IReadOnlyList<Guid>? dependencies = null)
    {
        var id = Guid.NewGuid();
        var body = BuildFlowBody(id, name, dependencies);
        var response = await client.PostAsJsonAsync("/smartconnect/v1/admin/flows", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Create '{name}' returned {response.StatusCode}: {text}");
        return id;
    }

    private static async Task UpdateDependencies_Async(
        HttpClient client,
        Guid id,
        string name,
        IReadOnlyList<Guid> dependencies)
    {
        var body = BuildFlowBody(id, name, dependencies);
        var response = await client.PutAsJsonAsync($"/smartconnect/v1/admin/flows/{id}", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Update '{name}' returned {response.StatusCode}: {text}");
    }

    private static object BuildFlowBody(Guid id, string name, IReadOnlyList<Guid>? dependencies) => new
    {
        id,
        name,
        runtimeState = 0,
        description = (string?)null,
        tags = Array.Empty<string>(),
        dataTypes = new[] { "HL7v2" },
        dependencies = dependencies ?? [],
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
}
