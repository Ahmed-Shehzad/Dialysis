using Transponder.Transports.Abstractions;

namespace Transponder.Transports.RabbitMq.Tests;

public sealed class RabbitMqTransportTests
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

    private sealed class SampleMessage
    {
    }

    [Fact]
    public void RabbitMqTopology_Uses_Defaults()
    {
        var topology = new RabbitMqTopology();
        var address = new Uri("rabbitmq://broker/orders");

        Assert.Equal("fanout", topology.ExchangeType);
        Assert.Equal("orders", topology.GetQueueName(address));
        Assert.Equal("SampleMessage", topology.GetExchangeName(typeof(SampleMessage)));
        Assert.Equal("SampleMessage", topology.GetRoutingKey(typeof(SampleMessage)));
    }

    [Fact]
    public void RabbitMqHostSettings_Requires_Host() => _ = Assert.Throws<ArgumentException>(() => new RabbitMqHostSettings(new Uri("rabbitmq://broker"), " "));

    [Fact]
    public void RabbitMqTransportFactory_Throws_For_Wrong_Settings()
    {
        var factory = new RabbitMqTransportFactory();
        var settings = new StubTransportHostSettings(new Uri("rabbitmq://broker"));

        _ = Assert.Throws<ArgumentException>(() => factory.CreateHost(settings));
    }
}
