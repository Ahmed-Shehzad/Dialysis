using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Scripts;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ExternalScriptRouteFilterTests
{
    [Fact]
    public async Task Loads_script_from_uri_and_returns_Allow_for_truthy()
    {
        var loader = new StubLoader("payloadText.indexOf('keep') >= 0");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = CreateMessage("""{"scriptUri":"file:///fake.js"}""", payload: "keep me");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
        Assert.Single(loader.Requests);
        Assert.Equal("file:///fake.js", loader.Requests[0].uri.AbsoluteUri);
    }

    [Fact]
    public async Task Returns_Drop_when_script_is_falsy()
    {
        var loader = new StubLoader("false");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = CreateMessage("""{"scriptUri":"https://scripts.example.com/f.js"}""");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Passes_cacheTtl_to_loader()
    {
        var loader = new StubLoader("true");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = CreateMessage("""{"scriptUri":"file:///fake.js","cacheTtlSeconds":30}""");

        await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(30), loader.Requests[0].ttl);
    }

    [Fact]
    public async Task Missing_scriptUri_allows_through_without_calling_loader()
    {
        var loader = new StubLoader("false");
        var filter = new ExternalScriptRouteFilter(loader);
        var msg = CreateMessage("""{"cacheTtlSeconds":30}""");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
        Assert.Empty(loader.Requests);
    }

    [Fact]
    public async Task No_parameters_metadata_allows_through()
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
