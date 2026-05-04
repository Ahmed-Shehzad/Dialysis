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
    public async Task PostFlow_start_pause_resume_cycle()
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

        var post = await client.PostAsJsonAsync("/smartconnect/v1/admin/flows", flow);
        post.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<IntegrationFlow>>("/smartconnect/v1/admin/flows");
        Assert.NotNull(list);
        Assert.Contains(list, f => f.Id == flowId);

        Assert.True((await client.PostAsync($"/smartconnect/v1/admin/flows/{flowId}/start", null)).IsSuccessStatusCode);
        Assert.True((await client.PostAsync($"/smartconnect/v1/admin/flows/{flowId}/pause", null)).IsSuccessStatusCode);
        var paused = await client.GetFromJsonAsync<IntegrationFlow>($"/smartconnect/v1/admin/flows/{flowId}");
        Assert.Equal(FlowRuntimeState.Paused, paused!.RuntimeState);

        Assert.True((await client.PostAsync($"/smartconnect/v1/admin/flows/{flowId}/start", null)).IsSuccessStatusCode);
        var started = await client.GetFromJsonAsync<IntegrationFlow>($"/smartconnect/v1/admin/flows/{flowId}");
        Assert.Equal(FlowRuntimeState.Started, started!.RuntimeState);

        var export = await client.GetAsync($"/smartconnect/v1/admin/flows/{flowId}/export");
        export.EnsureSuccessStatusCode();
        var json = await export.Content.ReadAsStringAsync();
        Assert.Contains(flowId.ToString(), json, StringComparison.Ordinal);

        using var importClient = factory.CreateClient();
        var import = await importClient.PostAsync(
            "/smartconnect/v1/admin/flows/import",
            new StringContent(json, Encoding.UTF8, "application/json"));
        import.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Ledger_query_returns_entries_after_dispatch()
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
        (await client.PostAsJsonAsync("/smartconnect/v1/admin/flows", flow)).EnsureSuccessStatusCode();

        var msg = new ByteArrayContent("hi"u8.ToArray());
        var inbound = await client.PostAsync(
            $"/smartconnect/v1/flows/{flowId}/messages",
            msg);
        inbound.EnsureSuccessStatusCode();

        var ledger = await client.GetFromJsonAsync<JsonElement>($"/smartconnect/v1/ledger/entries?flowId={flowId}");
        Assert.True(ledger.GetProperty("total").GetInt32() >= 1);
    }
}
