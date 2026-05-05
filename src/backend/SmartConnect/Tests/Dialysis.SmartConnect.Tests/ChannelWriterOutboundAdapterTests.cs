using System.Collections.Immutable;
using System.Globalization;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Inbound;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ChannelWriterOutboundAdapterTests
{
    private sealed class CapturingTransport : IInboundTransport
    {
        public List<IntegrationMessage> Dispatched { get; } = [];

        public bool ReturnSuccess { get; set; } = true;

        public Task<InboundReceiveResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
        {
            Dispatched.Add(message);
            return Task.FromResult(new InboundReceiveResult
            {
                Succeeded = ReturnSuccess,
                Error = ReturnSuccess ? null : "downstream-failed",
            });
        }
    }

    private static (ChannelWriterOutboundAdapter adapter, CapturingTransport transport, ServiceProvider sp)
        BuildAdapter()
    {
        var transport = new CapturingTransport();
        var services = new ServiceCollection();
        services.AddScoped<IInboundTransport>(_ => transport);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return (new ChannelWriterOutboundAdapter(scopeFactory), transport, sp);
    }

    private static IntegrationMessage Build(string parametersJson, Guid sourceFlowId, string corr = "C") =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = sourceFlowId,
            CorrelationId = corr,
            Payload = "p"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = ImmutableDictionary<string, string>.Empty
                .Add(HttpOutboundAdapter.ParametersMetadataKey, parametersJson)
                .Add("custom", "value"),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task Dispatches_to_target_flow_and_increments_depth()
    {
        var (adapter, transport, sp) = BuildAdapter();
        await using var _ = sp;
        var target = Guid.NewGuid();
        var src = Guid.NewGuid();
        var msg = Build($$"""{"TargetFlowId":"{{target}}","PreserveCorrelationId":true,"MetadataPropagation":0}""", src);

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorDetail);
        var dispatched = Assert.Single(transport.Dispatched);
        Assert.Equal(target, dispatched.FlowId);
        Assert.Equal("C", dispatched.CorrelationId);
        Assert.Equal("1", dispatched.Metadata[ChannelWriterOutboundAdapter.DepthMetadataKey]);
        Assert.Equal(src.ToString(), dispatched.Metadata[ChannelWriterOutboundAdapter.SourceFlowIdMetadataKey]);
        Assert.Equal("value", dispatched.Metadata["custom"]);
        Assert.False(dispatched.Metadata.ContainsKey(HttpOutboundAdapter.ParametersMetadataKey));
    }

    [Fact]
    public async Task Self_loop_is_refused()
    {
        var (adapter, _, sp) = BuildAdapter();
        await using var _2 = sp;
        var same = Guid.NewGuid();
        var msg = Build($$"""{"TargetFlowId":"{{same}}"}""", same);

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("self-loop", result.ErrorDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Depth_limit_aborts_chain()
    {
        var (adapter, transport, sp) = BuildAdapter();
        await using var _ = sp;
        var target = Guid.NewGuid();
        var src = Guid.NewGuid();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = src,
            CorrelationId = "C",
            Payload = "p"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = ImmutableDictionary<string, string>.Empty
                .Add(HttpOutboundAdapter.ParametersMetadataKey,
                    $$"""{"TargetFlowId":"{{target}}","MaxDepth":2}""")
                .Add(ChannelWriterOutboundAdapter.DepthMetadataKey, "2"),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(transport.Dispatched);
    }

    [Fact]
    public async Task Whitelist_only_propagates_named_metadata()
    {
        var (adapter, transport, sp) = BuildAdapter();
        await using var _ = sp;
        var target = Guid.NewGuid();
        var src = Guid.NewGuid();
        var json = $$"""{"TargetFlowId":"{{target}}","MetadataPropagation":2,"MetadataKeys":["custom"]}""";
        var msg = Build(json, src);

        await adapter.SendAsync(msg, 0, CancellationToken.None);

        var dispatched = Assert.Single(transport.Dispatched);
        Assert.Equal("value", dispatched.Metadata["custom"]);
        Assert.False(dispatched.Metadata.ContainsKey(HttpOutboundAdapter.ParametersMetadataKey));
        // Depth and source markers are always added by the adapter.
        Assert.True(dispatched.Metadata.ContainsKey(ChannelWriterOutboundAdapter.DepthMetadataKey));
    }

    [Fact]
    public async Task Downstream_failure_propagates_as_send_failure()
    {
        var (adapter, transport, sp) = BuildAdapter();
        await using var _ = sp;
        transport.ReturnSuccess = false;
        var target = Guid.NewGuid();
        var msg = Build($$"""{"TargetFlowId":"{{target}}"}""", Guid.NewGuid());

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("downstream-failed", result.ErrorDetail);
    }
}
