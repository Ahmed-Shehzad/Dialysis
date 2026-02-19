using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.TimeSync;

/// <summary>
/// Extension methods to add NTP sync health check.
/// </summary>
public static class TimeSyncHealthCheckExtensions
{
    private const string NtpSyncCheckName = "ntp-sync";

    /// <summary>
    /// Adds <see cref="NtpSyncHealthCheck"/> to verify system clock is NTP-synchronized (IHE Consistent Time).
    /// Returns Degraded (not Unhealthy) when status is unknown or not synced.
    /// </summary>
    public static IHealthChecksBuilder AddNtpSyncCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<NtpSyncHealthCheck>(NtpSyncCheckName, failureStatus: HealthStatus.Degraded);
    }
}
