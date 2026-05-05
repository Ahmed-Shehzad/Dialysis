using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.Hosting;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class SourceConnectorRegistryTests
{
    private sealed class StubConnector(string kind) : ISourceConnector
    {
        public string Kind { get; } = kind;

        public Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public void Register_then_resolve_by_kind_is_case_insensitive()
    {
        var registry = new SourceConnectorRegistry();
        var connector = new StubConnector("file-reader");

        registry.Register(connector);

        Assert.Same(connector, registry.TryResolve("FILE-READER"));
    }

    [Fact]
    public void TryResolve_returns_null_for_unknown_or_empty_kind()
    {
        var registry = new SourceConnectorRegistry();
        Assert.Null(registry.TryResolve("nope"));
        Assert.Null(registry.TryResolve(""));
    }

    [Fact]
    public void Register_with_blank_kind_throws()
    {
        var registry = new SourceConnectorRegistry();
        Assert.Throws<ArgumentException>(() => registry.Register(new StubConnector("   ")));
    }

    [Fact]
    public void Register_replaces_existing_kind_entry()
    {
        var registry = new SourceConnectorRegistry();
        var first = new StubConnector("k1");
        var second = new StubConnector("k1");

        registry.Register(first);
        registry.Register(second);

        Assert.Same(second, registry.TryResolve("k1"));
        Assert.Single(registry.All);
    }

    [Fact]
    public void All_returns_every_registered_connector()
    {
        var registry = new SourceConnectorRegistry();
        registry.Register(new StubConnector("a"));
        registry.Register(new StubConnector("b"));

        var kinds = registry.All.Select(c => c.Kind).OrderBy(k => k).ToArray();
        Assert.Equal(new[] { "a", "b" }, kinds);
    }
}
