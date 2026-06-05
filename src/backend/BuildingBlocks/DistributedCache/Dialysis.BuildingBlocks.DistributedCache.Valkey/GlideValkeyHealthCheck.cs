using Microsoft.Extensions.Diagnostics.HealthChecks;
using Valkey.Glide;

namespace Dialysis.BuildingBlocks.DistributedCache.Valkey;

/// <summary>
/// Readiness health check that PINGs Valkey through the GLIDE connection. Replaces the
/// StackExchange.Redis-bound <c>AddRedis</c> check from AspNetCore.HealthChecks.Redis.
/// </summary>
internal sealed class GlideValkeyHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _multiplexer;

    public GlideValkeyHealthCheck(IConnectionMultiplexer multiplexer)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        _multiplexer = multiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _multiplexer.GetDatabase().PingAsync().ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Valkey unreachable.", ex);
        }
    }
}
