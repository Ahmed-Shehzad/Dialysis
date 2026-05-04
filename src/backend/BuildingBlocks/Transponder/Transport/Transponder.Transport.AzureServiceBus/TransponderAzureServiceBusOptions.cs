namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

/// <summary>Azure Service Bus topic ingress (routing key in <see cref="TransponderTransportHeaderNames"/> application properties).</summary>
public sealed class TransponderAzureServiceBusOptions
{
    /// <summary>Namespace connection string (send + listen on topic subscription).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Topic that receives all Transponder publishes.</summary>
    public string TopicName { get; set; } = "transponder";

    /// <summary>Subscription under <see cref="TopicName"/> this host consumes.</summary>
    public string SubscriptionName { get; set; } = "default";

    /// <summary>Prefetch count for the topic subscription processor (0 = SDK default).</summary>
    public int PrefetchCount { get; set; } = 0;

    /// <summary>Max concurrent callbacks for the processor.</summary>
    public int MaxConcurrentCalls { get; set; } = 1;
}
