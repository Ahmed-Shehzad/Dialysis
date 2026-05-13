using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowRuntimeEngineAttachmentTests
{
    [Fact]
    public async Task Regex_handler_extracts_and_reattach_route_inflates_outbound()
    {
        var (sp, capture) = await BuildAsync();
        var flowId = Guid.CreateVersion7();
        await SeedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            AttachmentHandler = new AttachmentHandlerSlot
            {
                Kind = "regex",
                MimeType = "application/octet-stream",
                PropertiesJson = """{"pattern":"\\[([^\\]]+)\\]","mimeType":"text/plain"}""",
            },
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    ReattachAttachments = true,
                },
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    ReattachAttachments = false,
                },
            ],
        });

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("HEAD[PAYLOAD]TAIL"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, capture.Sent.Count);

        // Route 0: reattach=true → original capture-group bytes restored in place (brackets stay because
        // they were outside the capture group).
        Assert.Equal("HEAD[PAYLOAD]TAIL", Encoding.UTF8.GetString(capture.Sent[0].Payload.Span));
        // Route 1: reattach=false → token preserved between the brackets.
        var route1 = Encoding.UTF8.GetString(capture.Sent[1].Payload.Span);
        Assert.Contains("${ATTACH:", route1);
        Assert.DoesNotContain("PAYLOAD", route1);
        Assert.StartsWith("HEAD[", route1);
        Assert.EndsWith("]TAIL", route1);

        // Attachment row persisted for the message.
        var store = scope.ServiceProvider.GetRequiredService<IAttachmentStore>();
        var stored = await store.GetForMessageAsync(msg.Id, CancellationToken.None);
        Assert.Single(stored);
        Assert.Equal("PAYLOAD", Encoding.UTF8.GetString(stored[0].Data.Span));
    }

    [Fact]
    public async Task No_handler_slot_passes_message_through_unchanged()
    {
        var (sp, capture) = await BuildAsync();
        var flowId = Guid.CreateVersion7();
        await SeedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue },
            ],
        });

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("plain"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.Single(capture.Sent);
        Assert.Equal("plain", Encoding.UTF8.GetString(capture.Sent[0].Payload.Span));
    }

    private static async Task<(ServiceProvider sp, CapturingOutboundAdapter capture)> BuildAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_runtime_att_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        registry.RegisterOutboundAdapter(capture);
        return (sp, capture);
    }

    private static async Task SeedFlowAsync(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "attachment-test",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }
}
