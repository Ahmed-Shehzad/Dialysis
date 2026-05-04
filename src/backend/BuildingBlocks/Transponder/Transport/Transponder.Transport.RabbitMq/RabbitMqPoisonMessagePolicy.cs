namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// How failed deliveries are handled at the AMQP layer after a consumer handler throws.
/// </summary>
public enum RabbitMqPoisonMessagePolicy
{
    /// <summary>Negative-acknowledge with <c>requeue=true</c> (broker redelivers).</summary>
    Requeue,

    /// <summary>Negative-acknowledge with <c>requeue=false</c>. When a dead-letter fanout exchange is configured on the queue, messages route there instead of being dropped.</summary>
    DeadLetter,
}
