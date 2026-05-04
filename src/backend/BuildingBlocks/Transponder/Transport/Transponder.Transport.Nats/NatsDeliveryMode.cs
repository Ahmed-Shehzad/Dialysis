namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Core NATS (fire-and-forget) vs JetStream (durable streams, acks, at-least-once).
/// </summary>
public enum NatsDeliveryMode
{
    /// <summary>Classic publish / subscribe on <see cref="TransponderNatsOptions.IngressSubject"/>.</summary>
    Core,

    /// <summary>JetStream publish with ack; consume via durable pull consumer with explicit ack/nak.</summary>
    JetStream,
}
