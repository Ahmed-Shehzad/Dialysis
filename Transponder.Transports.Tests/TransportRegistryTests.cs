using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Tests;

public sealed class TransportRegistryTests
{
    private sealed class TestTransportFactory : ITransportFactory
    {
        public string Name => "Test";
        public IReadOnlyCollection<string> SupportedSchemes { get; } = ["foo"];
        public ITransportHost CreateHost(ITransportHostSettings settings) => throw new NotImplementedException();
    }

    [Fact]
    public void Register_Deduplicates_Factories()
    {
        var registry = new TransportRegistry();
        var factory = new TestTransportFactory();

        registry.Register(factory);
        registry.Register(factory);

        _ = Assert.Single(registry.Factories);
    }

    [Fact]
    public void TryResolve_Matches_Scheme_Ignoring_Case()
    {
        var factory = new TestTransportFactory();
        var registry = new TransportRegistry([factory]);

        bool resolved = registry.TryResolve(new Uri("FOO://localhost"), out ITransportFactory? resolvedFactory);

        Assert.True(resolved);
        Assert.Same(factory, resolvedFactory);
    }
}
