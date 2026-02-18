using System.Text.Json;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Intercessor.Behaviours;

/// <inheritdoc />
public class RedisCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery<TResponse>
{
    private readonly IDatabase _redisDb;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private readonly ILogger<RedisCachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCachingBehavior{TRequest, TResponse}"/> class,
    /// which provides caching capabilities using Redis to store and retrieve request/response data.
    /// </summary>
    /// <param name="redis">
    /// The Redis connection multiplexer used to access the Redis database.
    /// </param>
    /// <param name="logger">
    /// The logger used to log cache operations such as hits, misses, and exceptions.
    /// </param>
    public RedisCachingBehavior(IConnectionMultiplexer redis, ILogger<RedisCachingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _redisDb = redis.GetDatabase();
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        string key = request.CacheKey;

        if (string.IsNullOrWhiteSpace(key)) _ = await next();

        RedisValue cachedData = await _redisDb.StringGetAsync(key);
        if (cachedData.HasValue)
        {
            _logger.LogTrace("[Redis] Cache hit for {Name}", typeof(TRequest).Name);
            return JsonSerializer.Deserialize<TResponse>(cachedData.ToString())!;
        }

        _logger.LogTrace("[Redis] Cache miss for {Name}", typeof(TRequest).Name);
        TResponse response = await next();

        _ = await _redisDb.StringSetAsync(key, JsonSerializer.Serialize(response), request.CacheDuration ?? _cacheDuration);
        return response;
    }
}
