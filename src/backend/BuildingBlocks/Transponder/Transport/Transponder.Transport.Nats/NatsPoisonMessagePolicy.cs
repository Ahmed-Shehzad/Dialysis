namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// How handler failures are handled for core NATS (no broker ack like AMQP).
/// </summary>
public enum NatsPoisonMessagePolicy
{
    /// <summary>Log and continue (message is not redelivered by NATS core).</summary>
    Log,

    /// <summary>Republish the payload to <see cref="TransponderNatsOptions.PoisonSubject"/> with the same Transponder headers.</summary>
    Republish,
}
