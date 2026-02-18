namespace Transponder.Transports.RabbitMq;

/// <summary>
/// Groups connection-level settings for a RabbitMQ broker.
/// </summary>
public sealed record RabbitMqConnectionSettings(
    string Host,
    int Port = 5672,
    string VirtualHost = "/",
    bool UseTls = false,
    string? Username = null,
    string? Password = null,
    TimeSpan? RequestedHeartbeat = null);
