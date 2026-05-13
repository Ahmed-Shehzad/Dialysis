using System.Text;
using System.Text.Json;
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

public sealed class AddAttachmentJsApiTests
{
    [Fact]
    public async Task Transform_stage_addAttachment_persists_attachment_and_returns_token()
    {
        var (sp, capture) = await BuildAsync();
        var flowId = Guid.CreateVersion7();
        var script = """
            var token = addAttachment('hello-bytes', 'text/plain');
            'wrapped:' + token;
        """;
        await SeedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    TransformStages =
                    [
                        new TransformStageSlot
                        {
                            Kind = "javascript",
                            ParametersJson = JsonSerializer.Serialize(new { script }),
                        },
                    ],
                },
            ],
        });

        var messageId = Guid.CreateVersion7();
        var msg = new IntegrationMessage
        {
            Id = messageId,
            FlowId = flowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("orig"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(capture.Sent);
        var sentText = Encoding.UTF8.GetString(capture.Sent[0].Payload.Span);
        Assert.StartsWith("wrapped:${ATTACH:", sentText);

        var store = scope.ServiceProvider.GetRequiredService<IAttachmentStore>();
        var stored = await store.GetForMessageAsync(messageId, CancellationToken.None);
        Assert.Single(stored);
        Assert.Equal("hello-bytes", Encoding.UTF8.GetString(stored[0].Data.Span));
        Assert.Equal("text/plain", stored[0].MimeType);
    }

    private static async Task<(ServiceProvider sp, CapturingOutboundAdapter capture)> BuildAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_addatt_{Guid.NewGuid():N}");
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
            Name = "addatt-test",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }
}
