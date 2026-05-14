using System.Text;
using Dialysis.SmartConnect.Scripts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ChannelScriptExecutorTests
{
    private readonly InMemoryVariableMapStore _maps = new();
    private readonly ChannelScriptExecutor _executor ;

    public ChannelScriptExecutorTests()
    {
        _executor = new ChannelScriptExecutor(_maps, NullLogger<ChannelScriptExecutor>.Instance);
    }

    [Fact]
    public async Task Preprocessor_Return_False_Drops_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.Parse("00000000-0000-4000-8000-0000000000a1"),
            CorrelationId = "c",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var r = await _executor.RunPreProcessorAsync("false;", msg, CancellationToken.None);
        Assert.True(r.Dropped);
    }

    [Fact]
    public async Task Preprocessor_Return_String_Mutates_Payload_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.Parse("00000000-0000-4000-8000-0000000000a2"),
            CorrelationId = "c",
            Payload = "orig"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var r = await _executor.RunPreProcessorAsync("'next';", msg, CancellationToken.None);
        Assert.False(r.Dropped);
        Assert.Equal("next", Encoding.UTF8.GetString(r.NewPayload!));
    }

    [Fact]
    public async Task Preprocessor_Persists_Globalchannelmap_Put_Async()
    {
        // After the variable-maps refactor, the per-channel persisted scope is named globalChannelMap.
        // channelMap is now message-scoped (in-memory, not persisted), per Mirth semantics.
        var flowId = Guid.Parse("00000000-0000-4000-8000-0000000000a3");
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "c",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await _executor.RunPreProcessorAsync("globalChannelMap.put('k','v'); true;", msg, CancellationToken.None);
        var v = await _maps.GetAsync(VariableMapScope.GlobalChannel, flowId, "k", CancellationToken.None);
        Assert.Equal("v", v);
    }

    [Fact]
    public async Task Preprocessor_Channelmap_Is_Message_Scoped_Not_Persisted_Async()
    {
        var flowId = Guid.Parse("00000000-0000-4000-8000-0000000000a4");
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "c",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await _executor.RunPreProcessorAsync("channelMap.put('k','v'); true;", msg, CancellationToken.None);
        var v = await _maps.GetAsync(VariableMapScope.GlobalChannel, flowId, "k", CancellationToken.None);
        Assert.Null(v);
    }
}
