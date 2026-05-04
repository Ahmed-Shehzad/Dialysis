namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Settings for the NATS Transponder plugin (single ingress subject with Transponder headers).
/// </summary>
public sealed class TransponderNatsOptions
{
    /// <summary>NATS server URL (comma-separated list supported by the client).</summary>
    public string Url { get; set; } = "nats://localhost:4222";

    /// <summary>Client name reported to the server.</summary>
    public string ClientName { get; set; } = "Transponder";

    /// <summary>Subject all Transponder publishes use; routing key is carried in <see cref="TransponderTransportHeaderNames.RoutingKey"/>.</summary>
    public string IngressSubject { get; set; } = "transponder.send";

    /// <summary>Optional queue group (core) or JetStream <c>deliver_group</c> for competing consumers.</summary>
    public string? QueueGroup { get; set; }

    /// <summary>When <see cref="PoisonMessagePolicy"/> is <see cref="NatsPoisonMessagePolicy.Republish"/>, failed messages are published here.</summary>
    public string PoisonSubject { get; set; } = "transponder.dead";

    public NatsPoisonMessagePolicy PoisonMessagePolicy { get; set; } = NatsPoisonMessagePolicy.Log;

    /// <summary>Use core NATS or JetStream-backed delivery.</summary>
    public NatsDeliveryMode DeliveryMode { get; set; } = NatsDeliveryMode.Core;

    /// <summary>JetStream stream name (required when <see cref="DeliveryMode"/> is <see cref="NatsDeliveryMode.JetStream"/>).</summary>
    public string? JetStreamStream { get; set; }

    /// <summary>Durable consumer name (required when <see cref="DeliveryMode"/> is <see cref="NatsDeliveryMode.JetStream"/>).</summary>
    public string? JetStreamDurable { get; set; }

    /// <summary>
    /// When <see cref="DeliveryMode"/> is JetStream, creates or updates the stream so it includes <see cref="IngressSubject"/>
    /// (and <see cref="PoisonSubject"/> when <see cref="PoisonMessagePolicy"/> is <see cref="NatsPoisonMessagePolicy.Republish"/>).
    /// Turn off in production if streams are managed by infrastructure-as-code.
    /// </summary>
    public bool JetStreamAutoProvision { get; set; }
}
