using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.Hosting;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class InboundQueueConsumerTests
{
    [Fact]
    public async Task Queue_consumer_dispatches_to_runtime_and_ledger()
    {
        var flowId = Guid.Parse("00000000-0000-4000-8000-0000000000bb");
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_queue_{Guid.NewGuid():N}");
                services.AddSmartConnectCore();
                services.AddDefaultInboundMessageFactory();
                services.AddSmartConnectInboundTransport();
                services.AddSmartConnectChannelInboundQueueConsumer();
            })
            .Build();

        await host.StartAsync().ConfigureAwait(true);

        try
        {
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
                db.IntegrationFlows.Add(
                    new IntegrationFlowEntity
                    {
                        Id = flowId,
                        Name = "queue-test",
                        RuntimeState = (int)FlowRuntimeState.Started,
                        PipelineJson = PipelineJsonSerializer.Serialize(
                            new IntegrationFlowPipelineDefinition
                            {
                                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                                OutboundRoutes =
                                [
                                    new OutboundRouteSlot
                                    {
                                        OutboundAdapterKind = PassThroughOutboundAdapter.KindValue,
                                    },
                                ],
                            }),
                    });
                await db.SaveChangesAsync().ConfigureAwait(true);
            }

            var queue = host.Services.GetRequiredService<ChannelInboundQueue>();
            await queue.Writer.WriteAsync(
                new InboundQueueItem
                {
                    FlowId = flowId,
                    Payload = "queued"u8.ToArray(),
                    PayloadFormat = PayloadFormat.Utf8Text,
                    CorrelationId = "q-1",
                }).ConfigureAwait(true);

            var found = false;
            for (var attempt = 0; attempt < 40 && !found; attempt++)
            {
                await Task.Delay(50).ConfigureAwait(true);
                await using var verifyScope = host.Services.CreateAsyncScope();
                var dbVerify = verifyScope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
                var rows = await dbVerify.MessageLedgerEntries.AsNoTracking().ToListAsync().ConfigureAwait(true);
                found = rows.Exists(r => r.FlowId == flowId && r.CorrelationId == "q-1");
            }

            Assert.True(found);
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(true);
        }
    }
}
