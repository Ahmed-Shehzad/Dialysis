namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// Connection and topology settings for the RabbitMQ Transponder plugin.
/// </summary>
public sealed class TransponderRabbitMqOptions
{
    /// <summary>AMQP URI, for example <c>amqp://guest:guest@localhost:5672/</c>.</summary>
    public string ConnectionUri { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>Direct exchange used for typed routing keys.</summary>
    public string ExchangeName { get; set; } = "transponder";

    /// <summary>Application queue bound to the exchange for subscribed message types.</summary>
    public string QueueName { get; set; } = "transponder.default";

    /// <summary>
    /// When true, the publish channel is created with publisher confirmations; <c>IChannel.BasicPublishAsync</c> surfaces broker nacks as exceptions.
    /// </summary>
    public bool PublisherConfirmsEnabled { get; set; } = true;

    /// <summary>
    /// Optional fanout exchange that receives messages after negative acknowledgement with <c>requeue=false</c> when <see cref="PoisonMessagePolicy"/> is <see cref="RabbitMqPoisonMessagePolicy.DeadLetter"/>.
    /// Declare a matching durable queue and bind it to this exchange (empty routing key) to form a poison / DLQ pair.
    /// </summary>
    public string? DeadLetterFanoutExchangeName { get; set; }

    /// <summary>Queue that stores poison messages; must be bound to <see cref="DeadLetterFanoutExchangeName"/>.</summary>
    public string? DeadLetterQueueName { get; set; }

    /// <summary>Handler failure policy for AMQP negative acknowledgements.</summary>
    public RabbitMqPoisonMessagePolicy PoisonMessagePolicy { get; set; } = RabbitMqPoisonMessagePolicy.Requeue;

    /// <summary>
    /// Queue type for the application queue. <see cref="RabbitMqQueueType.Classic"/> matches
    /// the historical default (single-node, mirror queues if clustered — deprecated in 3.13+).
    /// Set to <see cref="RabbitMqQueueType.Quorum"/> when the broker is a clustered
    /// <c>RabbitmqCluster</c> CR with 3+ replicas; quorum queues are Raft-replicated and
    /// disk-flushed on each replica, surviving a single-node failure with no data loss.
    /// Classic queues stay backwards-compatible — flipping to Quorum is an operator decision
    /// per environment, gated on the broker actually being clustered.
    /// </summary>
    public RabbitMqQueueType QueueType { get; set; } = RabbitMqQueueType.Classic;
}

/// <summary>
/// Underlying queue implementation in RabbitMQ. Quorum is the modern HA primitive; classic
/// mirroring is deprecated. Quorum requires RMQ 3.8+ which the existing image already meets.
/// </summary>
public enum RabbitMqQueueType
{
    Classic,
    Quorum,
}
