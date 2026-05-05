using System.Net.Http.Json;
using System.Text.Json;
using Dialysis.SmartConnect.BuiltInPlugins;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class MessageBrowserApiTests
{
    [Fact]
    public async Task Messages_endpoint_returns_paginated_results()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var flowId = Guid.NewGuid();
        var flow = new IntegrationFlow
        {
            Id = flowId,
            Name = "browser-test",
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

        // Dispatch a message
        var msg = new ByteArrayContent("test-payload"u8.ToArray());
        (await client.PostAsync($"/smartconnect/v1/flows/{flowId}/messages", msg)).EnsureSuccessStatusCode();

        // Query messages
        var resp = await client.GetFromJsonAsync<JsonElement>(
            $"/smartconnect/v1/admin/messages?flowId={flowId}&take=10");

        Assert.True(resp.GetProperty("totalCount").GetInt32() >= 1);
        Assert.True(resp.GetProperty("items").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Get_message_by_id_returns_entry()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var flowId = Guid.NewGuid();
        var flow = new IntegrationFlow
        {
            Id = flowId,
            Name = "get-one",
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

        var msg = new ByteArrayContent("payload-by-id"u8.ToArray());
        (await client.PostAsync($"/smartconnect/v1/flows/{flowId}/messages", msg)).EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<JsonElement>(
            $"/smartconnect/v1/admin/messages?flowId={flowId}&take=5");
        var first = list.GetProperty("items")[0];
        var entryId = first.GetProperty("id").GetGuid();

        var one = await client.GetFromJsonAsync<JsonElement>($"/smartconnect/v1/admin/messages/{entryId}");
        Assert.Equal(entryId, one.GetProperty("id").GetGuid());
        Assert.Equal(flowId, one.GetProperty("flowId").GetGuid());
    }

    [Fact]
    public async Task Flow_statistics_returns_counts()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var flowId = Guid.NewGuid();
        var flow = new IntegrationFlow
        {
            Id = flowId,
            Name = "stats-test",
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

        var msg = new ByteArrayContent("data"u8.ToArray());
        (await client.PostAsync($"/smartconnect/v1/flows/{flowId}/messages", msg)).EnsureSuccessStatusCode();

        var stats = await client.GetFromJsonAsync<JsonElement>(
            $"/smartconnect/v1/admin/flows/{flowId}/statistics");

        Assert.True(stats.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Reprocess_nonexistent_entry_returns_404()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var resp = await client.PostAsync(
            $"/smartconnect/v1/admin/messages/{Guid.NewGuid()}/reprocess", null);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }
}
