using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Scripts;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ExternalScriptTransformStageTests
{
    [Fact]
    public async Task Loads_script_from_uri_and_replaces_payload()
    {
        var loader = new StubLoader("payloadText.toUpperCase()");
        var stage = new ExternalScriptTransformStage(loader);
        var msg = CreateMessage("""{"scriptUri":"file:///t.js"}""", payload: "hello");

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal("HELLO", Encoding.UTF8.GetString(result.Payload.Span));
        Assert.Equal(PayloadFormat.Utf8Text, result.PayloadFormat);
    }

    [Fact]
    public async Task Missing_scriptUri_returns_input_unchanged()
    {
        var loader = new StubLoader("'CHANGED'");
        var stage = new ExternalScriptTransformStage(loader);
        var msg = CreateMessage("""{"cacheTtlSeconds":10}""", payload: "original");

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal("original", Encoding.UTF8.GetString(result.Payload.Span));
        Assert.Empty(loader.Requests);
    }

    [Fact]
    public async Task Passes_cacheTtl_to_loader()
    {
        var loader = new StubLoader("'x'");
        var stage = new ExternalScriptTransformStage(loader);
        var msg = CreateMessage("""{"scriptUri":"file:///t.js","cacheTtlSeconds":120}""");

        await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(120), loader.Requests[0].ttl);
    }

    private static IntegrationMessage CreateMessage(string parametersJson, string payload = "test")
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
