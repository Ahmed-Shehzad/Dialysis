using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.BuiltInPlugins;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ManagementApiTests
{
    [Fact]
    public async Task Postflow_Start_Pause_Resume_Cycle_Async()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var flowId = Guid.NewGuid();
        var flow = new IntegrationFlow
        {
            Id = flowId,
            Name = "mgmt-test",
            RuntimeState = FlowRuntimeState.Stopped,
            Pipeline = new IntegrationFlowPipelineDefinition
            {
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes =
                [
                    new OutboundRouteSlot
                    {
                        OutboundAdapterKind = PassThroughOutboundAdapter.KindValue,
                        TransformStages = [],
                    },
                ],
            },
        };

        var post = await client.PostAsJsonAsync("/api/v1/admin/flows", flow);
        post.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<IntegrationFlow>>("/api/v1/admin/flows");
        Assert.NotNull(list);
        Assert.Contains(list, f => f.Id == flowId);

        Assert.True((await client.PostAsync($"/api/v1/admin/flows/{flowId}/start", null)).IsSuccessStatusCode);
        Assert.True((await client.PostAsync($"/api/v1/admin/flows/{flowId}/pause", null)).IsSuccessStatusCode);
        var paused = await client.GetFromJsonAsync<IntegrationFlow>($"/api/v1/admin/flows/{flowId}");
        Assert.Equal(FlowRuntimeState.Paused, paused!.RuntimeState);

        Assert.True((await client.PostAsync($"/api/v1/admin/flows/{flowId}/start", null)).IsSuccessStatusCode);
        var started = await client.GetFromJsonAsync<IntegrationFlow>($"/api/v1/admin/flows/{flowId}");
        Assert.Equal(FlowRuntimeState.Started, started!.RuntimeState);

        var export = await client.GetAsync($"/api/v1/admin/flows/{flowId}/export");
        export.EnsureSuccessStatusCode();
        var json = await export.Content.ReadAsStringAsync();
        Assert.Contains(flowId.ToString(), json, StringComparison.Ordinal);

        using var importClient = factory.CreateClient();
        using var importContent = new StringContent(json, Encoding.UTF8, "application/json");
        var import = await importClient.PostAsync(
            "/api/v1/admin/flows/import",
            importContent);
        import.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Export_Group_Returns_Json_Async()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var groupId = Guid.NewGuid();
        var create = await client.PostAsJsonAsync(
            "/api/v1/admin/groups",
            new FlowGroup { Id = groupId, Name = "g1", Description = "d1" });
        create.EnsureSuccessStatusCode();

        var export = await client.GetAsync($"/api/v1/admin/groups/{groupId}/export");
        export.EnsureSuccessStatusCode();
        var json = await export.Content.ReadAsStringAsync();
        Assert.Contains(groupId.ToString("D"), json, StringComparison.Ordinal);
        Assert.Contains("g1", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ledger_Query_Returns_Entries_After_Dispatch_Async()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var flowId = Guid.NewGuid();
        var flow = new IntegrationFlow
        {
            Id = flowId,
            Name = "ledger-api-test",
            RuntimeState = FlowRuntimeState.Started,
            Pipeline = new IntegrationFlowPipelineDefinition
            {
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes =
                [
                    new OutboundRouteSlot { OutboundAdapterKind = PassThroughOutboundAdapter.KindValue },
                ],
            },
        };
        (await client.PostAsJsonAsync("/api/v1/admin/flows", flow)).EnsureSuccessStatusCode();

        using var msg = new ByteArrayContent("hi"u8.ToArray());
        var inbound = await client.PostAsync(
            $"/smartconnect/v1/flows/{flowId}/messages",
            msg);
        inbound.EnsureSuccessStatusCode();

        var ledger = await client.GetFromJsonAsync<JsonElement>($"/smartconnect/v1/ledger/entries?flowId={flowId}");
        Assert.True(ledger.GetProperty("total").GetInt32() >= 1);
    }
}
