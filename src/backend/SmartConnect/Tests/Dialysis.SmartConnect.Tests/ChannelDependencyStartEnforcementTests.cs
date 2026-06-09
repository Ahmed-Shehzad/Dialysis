using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers POST /flows/{id}/start dependency enforcement:
///  - 422 + unmetDependencies payload when a declared dependency isn't Started.
///  - 204 once the dependency is Started.
///  - 204 immediately when ?force=true is supplied, even with stopped deps.
/// </summary>
public sealed class ChannelDependencyStartEnforcementTests : IClassFixture<SmartConnectApiFactory>
{
    private readonly SmartConnectApiFactory _factory;

    public ChannelDependencyStartEnforcementTests(SmartConnectApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Start_Returns_422_With_Unmet_Dependencies_Async()
    {
        using var client = _factory.CreateClient();
        var dependencyId = await CreateFlowAsync(client, name: "dep-stopped");
        var dependentId = await CreateFlowAsync(client, name: "dependent", dependencies: [dependencyId]);

        var response = await client.PostAsync($"/api/v1/admin/flows/{dependentId}/start", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var unmet = doc.RootElement.GetProperty("unmetDependencies");
        Assert.Equal(1, unmet.GetArrayLength());
        Assert.Equal(dependencyId.ToString(), unmet[0].GetProperty("id").GetString());
        Assert.Equal("Stopped", unmet[0].GetProperty("state").GetString());
    }

    [Fact]
    public async Task Start_Succeeds_Once_Dependencies_Are_Started_Async()
    {
        using var client = _factory.CreateClient();
        var dependencyId = await CreateFlowAsync(client, name: "dep-online");
        var dependentId = await CreateFlowAsync(client, name: "online-dependent", dependencies: [dependencyId]);

        // Start the dep first; then the dependent should accept.
        var depStart = await client.PostAsync($"/api/v1/admin/flows/{dependencyId}/start", content: null);
        Assert.Equal(HttpStatusCode.NoContent, depStart.StatusCode);

        var dependentStart = await client.PostAsync($"/api/v1/admin/flows/{dependentId}/start", content: null);
        Assert.Equal(HttpStatusCode.NoContent, dependentStart.StatusCode);
    }

    [Fact]
    public async Task Start_With_Force_Bypasses_Unmet_Dependencies_Async()
    {
        using var client = _factory.CreateClient();
        var dependencyId = await CreateFlowAsync(client, name: "still-stopped");
        var dependentId = await CreateFlowAsync(client, name: "forced", dependencies: [dependencyId]);

        var response = await client.PostAsync($"/api/v1/admin/flows/{dependentId}/start?force=true", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<Guid> CreateFlowAsync(
        HttpClient client,
        string name,
        IReadOnlyList<Guid>? dependencies = null)
    {
        var id = Guid.NewGuid();
        var body = new
        {
            id,
            name,
            runtimeState = 0, // Stopped at create
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
        var response = await client.PostAsJsonAsync("/api/v1/admin/flows", body);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Create flow '{name}' returned {response.StatusCode}: {responseBody}");
        return id;
    }
}
