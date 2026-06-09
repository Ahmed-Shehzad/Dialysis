using System.Net;
using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class InboundHttpIntegrationTests : IClassFixture<SmartConnectApiFactory>
{
    private readonly SmartConnectApiFactory _factory;

    public InboundHttpIntegrationTests(SmartConnectApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_Inbound_Message_Returns_200_And_Records_Ledger_Async()
    {
        using var client = _factory.CreateClient();
        var flowId = Guid.Parse("00000000-0000-4000-8000-0000000000aa");
        await Seedstartedflow_Async(
            flowId,
            new IntegrationFlowPipelineDefinition
            {
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes =
                [
                    new OutboundRouteSlot { OutboundAdapterKind = PassThroughOutboundAdapter.KindValue },
                ],
            }).ConfigureAwait(true);

        using var requestContent = new StringContent("hello", Encoding.UTF8, "text/plain");
        var response = await client.PostAsync(
            $"/smartconnect/v1/flows/{flowId}/messages",
            requestContent).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        var rows = await db.MessageLedgerEntries.AsNoTracking().ToListAsync().ConfigureAwait(true);
        Assert.Contains(rows, r => r.FlowId == flowId && r.Status == (int)MessageLedgerStatus.Received);
    }

    [Fact]
    public async Task Post_Unknown_Flow_Returns_404_Without_Dispatch_Body_Success_Async()
    {
        using var client = _factory.CreateClient();
        var flowId = Guid.Parse("00000000-0000-4000-8000-00000000dead");
        using var requestContent = new StringContent("x", Encoding.UTF8, "text/plain");
        var response = await client.PostAsync(
            $"/smartconnect/v1/flows/{flowId}/messages",
            requestContent).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task Seedstartedflow_Async(Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(
            new IntegrationFlowEntity
            {
                Id = flowId,
                Name = "http-test-flow",
                RuntimeState = (int)FlowRuntimeState.Started,
                PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
            });
        await db.SaveChangesAsync().ConfigureAwait(true);
    }
}
