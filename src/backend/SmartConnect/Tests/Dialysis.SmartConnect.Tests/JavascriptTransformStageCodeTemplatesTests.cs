using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JavascriptTransformStageCodeTemplatesTests
{
    [Fact]
    public async Task User_script_calls_function_defined_in_linked_library()
    {
        var (sp, capture) = await BuildAsync();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        await repo.UpsertAsync(new CodeTemplateLibrary
        {
            Id = libraryId,
            Name = "TestHelpers",
            LinkedFlowIds = [flowId],
            Templates = new List<CodeTemplate>
            {
                new()
                {
                    Id = Guid.CreateVersion7(),
                    LibraryId = libraryId,
                    Name = "double",
                    Code = "function double(n){ return n * 2; }",
                    Contexts = [CodeTemplateContext.DestinationTransformer],
                    LastModifiedUtc = DateTimeOffset.UtcNow,
                },
            },
            LastModifiedUtc = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        var transformScript = JsonSerializer.Serialize(new { script = "String(double(21))" });
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
                        new TransformStageSlot { Kind = "javascript", ParametersJson = transformScript },
                    ],
                },
            ],
        });

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "ct-1",
            Payload = Encoding.UTF8.GetBytes("orig"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(capture.Sent);
        Assert.Equal("42", Encoding.UTF8.GetString(capture.Sent[0].Payload.Span));
    }

    private async static Task<(ServiceProvider sp, CapturingOutboundAdapter capture)> BuildAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_ctmpl_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        registry.RegisterOutboundAdapter(capture);
        return (sp, capture);
    }

    private async static Task SeedFlowAsync(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "ct-test",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }
}
