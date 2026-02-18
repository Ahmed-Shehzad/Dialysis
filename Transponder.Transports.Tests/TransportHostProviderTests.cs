using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Tests;

public sealed class TransportHostProviderTests
{
    private sealed class StubTransportHost : ITransportHost
    {
        public StubTransportHost(Uri address)
        {
            Address = address;
        }

        public Uri Address { get; }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IPublishTransport> GetPublishTransportAsync(Type messageType, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
            => throw new NotImplementedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void GetHost_Returns_Registered_Host_By_Scheme()
    {
        var host = new StubTransportHost(new Uri("foo://host"));
        var provider = new TransportHostProvider([host]);

        ITransportHost resolved = provider.GetHost(new Uri("foo://other"));

        Assert.Same(host, resolved);
    }

    [Fact]
    public void GetHost_Returns_Registered_Host_By_Authority()
    {
        var hostA = new StubTransportHost(new Uri("http://service-a:8080"));
        var hostB = new StubTransportHost(new Uri("http://service-b:8080"));
        var provider = new TransportHostProvider([hostA, hostB]);

        ITransportHost resolved = provider.GetHost(new Uri("http://service-b:8080/requests/ping"));

        Assert.Same(hostB, resolved);
    }

    [Fact]
    public void GetHost_Prefers_Most_Specific_Base_Path()
    {
        var rootHost = new StubTransportHost(new Uri("http://service:8080"));
        var apiHost = new StubTransportHost(new Uri("http://service:8080/api"));
        var provider = new TransportHostProvider([rootHost, apiHost]);

        ITransportHost resolved = provider.GetHost(new Uri("http://service:8080/api/requests/ping"));

        Assert.Same(apiHost, resolved);
    }

    [Fact]
    public void GetHost_Throws_When_Missing_Scheme()
    {
        var provider = new TransportHostProvider([]);

        _ = Assert.Throws<InvalidOperationException>(() => provider.GetHost(new Uri("missing://host")));
    }
}
