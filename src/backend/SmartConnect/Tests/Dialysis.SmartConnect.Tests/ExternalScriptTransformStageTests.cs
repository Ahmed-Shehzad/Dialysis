using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Scripts;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ExternalScriptTransformStageTests
{
    [Fact]
    public async Task Loads_Script_From_Uri_And_Replaces_Payload_Async()
    {
        var loader = new StubLoader("payloadText.toUpperCase()");
        var stage = new ExternalScriptTransformStage(loader);
        var msg = Create_Message("""{"scriptUri":"file:///t.js"}""", payload: "hello");

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal("HELLO", Encoding.UTF8.GetString(result.Payload.Span));
        Assert.Equal(PayloadFormat.Utf8Text, result.PayloadFormat);
    }

    [Fact]
    public async Task Missing_Scripturi_Returns_Input_Unchanged_Async()
    {
        var loader = new StubLoader("'CHANGED'");
        var stage = new ExternalScriptTransformStage(loader);
        var msg = Create_Message("""{"cacheTtlSeconds":10}""", payload: "original");

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal("original", Encoding.UTF8.GetString(result.Payload.Span));
        Assert.Empty(loader.Requests);
    }

    [Fact]
    public async Task Passes_Cachettl_To_Loader_Async()
    {
        var loader = new StubLoader("'x'");
        var stage = new ExternalScriptTransformStage(loader);
        var msg = Create_Message("""{"scriptUri":"file:///t.js","cacheTtlSeconds":120}""");

        await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(120), loader.Requests[0].ttl);
    }

    private static IntegrationMessage Create_Message(string parametersJson, string payload = "test")
    {
        return new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = ImmutableDictionary<string, string>.Empty.Add(
                ExternalScriptTransformStage.ParametersMetadataKey,
                parametersJson),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private sealed class StubLoader(string body) : IExternalScriptLoader
    {
        public List<(Uri uri, TimeSpan? ttl)> Requests { get; } = new();

        public Task<string> LoadAsync(Uri uri, TimeSpan? cacheTtl, CancellationToken cancellationToken)
        {
            Requests.Add((uri, cacheTtl));
            return Task.FromResult(body);
        }
    }
}
