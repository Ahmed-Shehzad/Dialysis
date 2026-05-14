using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Scripts;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ExternalScriptRouteFilterTests
{
    [Fact]
    public async Task Loads_Script_From_Uri_And_Returns_Allow_For_Truthy_Async()
    {
        var loader = new StubLoader("payloadText.indexOf('keep') >= 0");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = Create_Message("""{"scriptUri":"file:///fake.js"}""", payload: "keep me");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
        Assert.Single(loader.Requests);
        Assert.Equal("file:///fake.js", loader.Requests[0].uri.AbsoluteUri);
    }

    [Fact]
    public async Task Returns_Drop_When_Script_Is_Falsy_Async()
    {
        var loader = new StubLoader("false");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = Create_Message("""{"scriptUri":"https://scripts.example.com/f.js"}""");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Passes_Cachettl_To_Loader_Async()
    {
        var loader = new StubLoader("true");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = Create_Message("""{"scriptUri":"file:///fake.js","cacheTtlSeconds":30}""");

        await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(30), loader.Requests[0].ttl);
    }

    [Fact]
    public async Task Missing_Scripturi_Allows_Through_Without_Calling_Loader_Async()
    {
        var loader = new StubLoader("false");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = Create_Message("""{"cacheTtlSeconds":30}""");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
        Assert.Empty(loader.Requests);
    }

    [Fact]
    public async Task No_Parameters_Metadata_Allows_Through_Async()
    {
        var loader = new StubLoader("false");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
        Assert.Empty(loader.Requests);
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
                ExternalScriptRouteFilter.ParametersMetadataKey,
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
