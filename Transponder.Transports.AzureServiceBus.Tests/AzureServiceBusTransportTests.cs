using Transponder.Transports.Abstractions;

namespace Transponder.Transports.AzureServiceBus.Tests;

public sealed class AzureServiceBusTransportTests
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
    public void AzureServiceBusTopology_Resolves_Queue_And_Subscription()
    {
        var topology = new AzureServiceBusTopology();
        var address = new Uri("sb://namespace/topic/subscriptions/subscription-A");

        string queueName = topology.GetQueueName(address);
        string? subscription = topology.GetSubscriptionName(address);
        string topicName = topology.GetTopicName(typeof(SampleMessage));

        Assert.Equal("topic", queueName);
        Assert.Equal("subscription-A", subscription);
        Assert.Equal("SampleMessage", topicName);
    }

    [Fact]
    public void AzureServiceBusTransportFactory_Throws_For_Wrong_Settings()
    {
        var factory = new AzureServiceBusTransportFactory();
        var settings = new StubTransportHostSettings(new Uri("sb://namespace"));

        _ = Assert.Throws<ArgumentException>(() => factory.CreateHost(settings));
    }
}
