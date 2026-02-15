using StackExchange.Redis;

namespace Transponder.Persistence.Redis;

/// <summary>
/// Configures Redis caching for Transponder.
/// </summary>
public sealed class TransponderRedisCacheOptions
{
    public string? ConnectionString { get; set; }

    public string? InstanceName { get; set; }

    public bool AbortOnConnectFail { get; set; }

    public bool AllowUntrustedCertificates { get; set; }

    public TimeSpan? ConnectTimeout { get; set; }

    public TimeSpan? SyncTimeout { get; set; }

    public Func<Task<IConnectionMultiplexer>>? ConnectionMultiplexerFactory { get; set; }
}
