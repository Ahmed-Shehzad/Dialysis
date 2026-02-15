using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc.Tests;

public sealed class GrpcTransportTests
{
    private sealed class StubTransportHostSettings : ITransportHostSettings
    {
        public StubTransportHostSettings(Uri address)
        {
            Address = address;
        }

        public Uri Address { get; }
        public IReadOnlyDictionary<string, object?> Settings { get; } = new Dictionary<string, object?>();
    }

    [Fact]
    public void GrpcTopology_Uses_Default_Names()
    {
        var topology = new GrpcTopology();

        Assert.Equal("transponder.transport.Transport", topology.ServiceName);
        Assert.Equal("Send", topology.SendMethodName);
        Assert.Equal("Publish", topology.PublishMethodName);
    }

    [Fact]
    public void GrpcHostSettings_Defaults_Apply()
    {
        var settings = new GrpcHostSettings(new Uri("https://localhost"));

        Assert.True(settings.UseTls);
        Assert.NotNull(settings.Topology);
    }

    [Fact]
    public void GrpcTransportFactory_Throws_For_Wrong_Settings()
    {
        var factory = new GrpcTransportFactory();
        var settings = new StubTransportHostSettings(new Uri("https://localhost"));

        _ = Assert.Throws<ArgumentException>(() => factory.CreateHost(settings));
    }
}
