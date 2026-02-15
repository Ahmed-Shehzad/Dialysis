using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

namespace Transponder.Persistence.Redis;

/// <summary>
/// Extension methods to register Redis caching services.
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddTransponderRedisCache(
        this IServiceCollection services,
        Action<TransponderRedisCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TransponderRedisCacheOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("Redis connection string must be provided.", nameof(options));

        var configurationOptions = ConfigurationOptions.Parse(options.ConnectionString);
        configurationOptions.AbortOnConnectFail = options.AbortOnConnectFail;

        if (options.AllowUntrustedCertificates)
            configurationOptions.CertificateValidation += (_, _, _, _) => true;

        if (options.ConnectTimeout.HasValue)
            configurationOptions.ConnectTimeout = (int)options.ConnectTimeout.Value.TotalMilliseconds;

        if (options.SyncTimeout.HasValue)
            configurationOptions.SyncTimeout = (int)options.SyncTimeout.Value.TotalMilliseconds;

        _ = services.AddStackExchangeRedisCache(cacheOptions =>
        {
            cacheOptions.ConfigurationOptions = configurationOptions;

            if (!string.IsNullOrWhiteSpace(options.InstanceName))
                cacheOptions.InstanceName = options.InstanceName;

            if (options.ConnectionMultiplexerFactory is not null)
                cacheOptions.ConnectionMultiplexerFactory = options.ConnectionMultiplexerFactory;
        });

        return services;
    }
}
