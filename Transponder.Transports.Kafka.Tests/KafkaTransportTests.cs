using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Kafka.Tests;

public sealed class KafkaTransportTests
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
    public void KafkaTopology_Uses_Address_For_Topic_And_Group()
    {
        var topology = new KafkaTopology();
        var address = new Uri("kafka://broker/orders");

        string topicName = topology.GetTopicName(address);
        string group = topology.GetConsumerGroup(address);
        string typeTopic = topology.GetTopicName(typeof(SampleMessage));

        Assert.Equal("orders", topicName);
        Assert.Equal("broker-consumer", group);
        Assert.Equal("SampleMessage", typeTopic);
    }

    [Fact]
    public void KafkaHostSettings_Requires_Bootstrap_Servers() => _ = Assert.Throws<ArgumentException>(() => new KafkaHostSettings(new Uri("kafka://broker"), new List<string>()));

    [Fact]
    public void KafkaTransportFactory_Throws_For_Wrong_Settings()
    {
        var factory = new KafkaTransportFactory();
        var settings = new StubTransportHostSettings(new Uri("kafka://broker"));

        _ = Assert.Throws<ArgumentException>(() => factory.CreateHost(settings));
    }
}
