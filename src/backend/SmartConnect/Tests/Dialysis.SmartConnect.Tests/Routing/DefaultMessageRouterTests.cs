using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Routing;

/// <summary>
/// Covers the content-based router predicate matching and end-to-end resolution against an
/// in-memory flow repository.
/// </summary>
public sealed class DefaultMessageRouterTests
{
    [Theory]
    [InlineData("ORU^R*", "ORU^R01", true)]
    [InlineData("ORU^R*", "ORU^R40", true)]
    [InlineData("ORU^R*", "ADT^A01", false)]
    [InlineData("ADT^A01", "ADT^A01", true)]
    [InlineData("ADT^A01", "ADT^A04", false)]
    [InlineData("*", "ANY^X01", true)]
    public void MessageType_Pattern_Matches_Expected(string pattern, string messageType, bool expected)
    {
        var sub = new InboundSubscriptionSlot { MessageTypePattern = pattern };
        var candidate = new MessageRoutingCandidate("http", messageType, null, new Dictionary<string, string>());

        Assert.Equal(expected, DefaultMessageRouter.Matches(sub, candidate));
    }

    [Fact]
    public void Source_Kind_Filter_Skips_Non_Matching_Kind()
    {
        var sub = new InboundSubscriptionSlot { SourceKind = "mllp", MessageTypePattern = "*" };
        var httpCandidate = new MessageRoutingCandidate("http", "ORU^R01", null, new Dictionary<string, string>());
        var mllpCandidate = new MessageRoutingCandidate("mllp", "ORU^R01", null, new Dictionary<string, string>());

        Assert.False(DefaultMessageRouter.Matches(sub, httpCandidate));
        Assert.True(DefaultMessageRouter.Matches(sub, mllpCandidate));
    }

    [Fact]
    public async Task Resolves_Only_Started_Flows_With_Matching_Subscriptions_Async()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_router_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();
        await using var sp = services.BuildServiceProvider();

        var startedMatching = Guid.CreateVersion7();
        var startedNonMatching = Guid.CreateVersion7();
        var stoppedMatching = Guid.CreateVersion7();
        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
            await db.IntegrationFlows.AddRangeAsync(
                new IntegrationFlowEntity
                {
                    Id = startedMatching,
                    Name = "started-matching",
                    RuntimeState = (int)FlowRuntimeState.Started,
                    PipelineJson = PipelineJsonSerializer.Serialize(new IntegrationFlowPipelineDefinition
                    {
                        InboundSubscriptions =
                        [
                            new InboundSubscriptionSlot { SourceKind = "http", MessageTypePattern = "ORU^R*" },
                        ],
                    }),
                },
                new IntegrationFlowEntity
                {
                    Id = startedNonMatching,
                    Name = "started-non-matching",
                    RuntimeState = (int)FlowRuntimeState.Started,
                    PipelineJson = PipelineJsonSerializer.Serialize(new IntegrationFlowPipelineDefinition
                    {
                        InboundSubscriptions =
                        [
                            new InboundSubscriptionSlot { SourceKind = "mllp", MessageTypePattern = "ADT^*" },
                        ],
                    }),
                },
                new IntegrationFlowEntity
                {
                    Id = stoppedMatching,
                    Name = "stopped-matching",
                    RuntimeState = (int)FlowRuntimeState.Stopped,
                    PipelineJson = PipelineJsonSerializer.Serialize(new IntegrationFlowPipelineDefinition
                    {
                        InboundSubscriptions =
                        [
                            new InboundSubscriptionSlot { SourceKind = "http", MessageTypePattern = "*" },
                        ],
                    }),
                });
            await db.SaveChangesAsync();
        }

        var router = sp.GetRequiredService<IMessageRouter>();
        var candidate = new MessageRoutingCandidate("http", "ORU^R01", null, new Dictionary<string, string>());
        var matched = await router.ResolveFlowIdsAsync(candidate, CancellationToken.None);

        Assert.Single(matched);
        Assert.Equal(startedMatching, matched[0]);
    }
}
