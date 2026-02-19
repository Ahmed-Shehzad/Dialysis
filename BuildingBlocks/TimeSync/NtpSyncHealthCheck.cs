using System.Diagnostics;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.TimeSync;

/// <summary>
/// Health check that verifies system clock is NTP-synchronized (IHE Consistent Time alignment).
/// On Linux: uses timedatectl; on Windows: uses w32tm.
/// Returns Degraded if not synced or status unknown (does not fail health).
/// </summary>
public sealed class NtpSyncHealthCheck : IHealthCheck
{
    private const int ProcessTimeoutMs = 5000;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (OperatingSystem.IsLinux())
                return await CheckLinuxAsync(cancellationToken);
            if (OperatingSystem.IsWindows())
                return await CheckWindowsAsync(cancellationToken);

            return HealthCheckResult.Degraded("NTP sync check not supported on this platform (e.g. macOS). PDMS time depends on host sync.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Could not determine NTP sync status (e.g. timedatectl/w32tm unavailable in container).",
                exception: ex);
        }
    }

    private static async Task<HealthCheckResult> CheckLinuxAsync(CancellationToken cancellationToken)
    {
        try
        {
            string output = await RunProcessAsync("timedatectl", "show --property=NTPSynchronized", cancellationToken);
            if (string.IsNullOrWhiteSpace(output))
                return HealthCheckResult.Degraded("timedatectl returned no output.");

            bool synchronized = output.Trim().Equals("NTPSynchronized=yes", StringComparison.OrdinalIgnoreCase);
            return synchronized
                ? HealthCheckResult.Healthy("NTP synchronized.")
                : HealthCheckResult.Degraded("NTP not synchronized. Ensure NTP is configured (systemd-timesyncd, chrony, or ntpd).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Could not run timedatectl (may be unavailable in minimal container).",
                exception: ex);
        }
    }

    private static async Task<HealthCheckResult> CheckWindowsAsync(CancellationToken cancellationToken)
    {
        try
        {
            string output = await RunProcessAsync("w32tm", "/query /status", cancellationToken);
            if (string.IsNullOrWhiteSpace(output))
                return HealthCheckResult.Degraded("w32tm returned no output.");

            // "Source: Free-Running System Clock" = not synced
            // "Source: time.windows.com" or NTP server = synced
            if (output.Contains("Free-Running System Clock", StringComparison.OrdinalIgnoreCase))
                return HealthCheckResult.Degraded("Windows time service: Free-Running (not NTP synchronized).");

            if (output.Contains("Source:", StringComparison.OrdinalIgnoreCase) &&
                (output.Contains("time.", StringComparison.OrdinalIgnoreCase) ||
                 output.Contains("ntp.", StringComparison.OrdinalIgnoreCase) ||
                 output.Contains("Leap Indicator: 0", StringComparison.OrdinalIgnoreCase)))
                return HealthCheckResult.Healthy("NTP synchronized.");

            // Last Successful Sync Time present suggests sync occurred
            if (output.Contains("Last Successful Sync Time:", StringComparison.OrdinalIgnoreCase) &&
                !output.Contains("The computer has not yet tried to sync", StringComparison.OrdinalIgnoreCase))
                return HealthCheckResult.Healthy("Time service appears synchronized.");

            return HealthCheckResult.Degraded("NTP sync status unclear. Verify Windows Time service configuration.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Could not run w32tm.",
                exception: ex);
        }
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ProcessTimeoutMs);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            throw new InvalidOperationException($"Failed to start {fileName}.");

        try
        {
            string output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);
            return output;
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw;
        }
    }
}
