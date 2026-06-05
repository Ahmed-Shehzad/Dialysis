namespace Dialysis.Module.Bff.Events;

/// <summary>
/// Configuration for a BFF's event-driven push (section <c>Bff:Events</c>). All transport is
/// opt-in: with no RabbitMQ connection the BFF runs the in-process Transponder bus (dev/tests) and
/// the hub still maps but receives nothing cross-process; with no SignalR backplane the hub runs
/// in-process (single replica). Production sets both via the Aspire AppHost.
/// </summary>
public sealed class BffEventsOptions
{
    /// <summary>Configuration section this binds to.</summary>
    public const string SectionName = "Bff:Events";

    /// <summary>RabbitMQ transport settings for consuming integration events.</summary>
    public RabbitMqSection RabbitMq { get; set; } = new();

    /// <summary>SignalR scale-out settings.</summary>
    public SignalRSection SignalR { get; set; } = new();

    /// <summary>Explicit hub path. Defaults to <c>{BasePath}/events</c> when unset.</summary>
    public string? HubPath { get; set; }

    /// <summary>RabbitMQ consume transport.</summary>
    public sealed class RabbitMqSection
    {
        /// <summary>AMQP connection URI. When empty, the BFF does not attach to RabbitMQ (in-process bus).</summary>
        public string? ConnectionUri { get; set; }

        /// <summary>
        /// Queue this BFF consumes from. Defaults to <c>bff-{slug}</c>. Must be unique per context so
        /// each BFF binds its own copy of the producers' events and never competes with the owning
        /// module's consumers for deliveries.
        /// </summary>
        public string? QueueName { get; set; }
    }

    /// <summary>SignalR backplane.</summary>
    public sealed class SignalRSection
    {
        /// <summary>
        /// Valkey/Redis connection string for the SignalR backplane. When set, every BFF replica
        /// subscribes to the same pub/sub channel so a push routed by one replica reaches clients
        /// connected to another. When empty, SignalR runs in-process (single replica).
        /// </summary>
        public string? BackplaneConnectionString { get; set; }
    }
}
