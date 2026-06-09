namespace Dialysis.DataSimulator;

/// <summary>
/// Distinguishes a genuine host shutdown from a per-request <see cref="System.Net.Http.HttpClient"/>
/// timeout — which is the whole trick to keeping the background loops alive.
/// </summary>
/// <remarks>
/// <para>
/// <c>HttpClient.Timeout</c> elapsing throws a <see cref="System.Threading.Tasks.TaskCanceledException"/>,
/// and that type derives from <see cref="System.OperationCanceledException"/>. So a slow endpoint is, by
/// type alone, indistinguishable from a cooperative stop — a naive <c>catch (Exception ex) when (ex is not
/// OperationCanceledException)</c> filter lets the timeout escape, faults the <c>BackgroundService</c>, and
/// (under the default <c>BackgroundServiceExceptionBehavior.StopHost</c>) tears the whole simulator down.
/// That is exactly the cascade we want to prevent: one 100s timeout must not kill the process.
/// </para>
/// <para>
/// The reliable discriminator is the stopping token: only a genuine shutdown has it cancelled. A request
/// timeout fires an unrelated internal token while ours stays live. So treat an
/// <see cref="System.OperationCanceledException"/> as a shutdown <em>only</em> when our stopping token is
/// the one that tripped; everything else — including a timeout — is a transient, swallowable failure.
/// </para>
/// </remarks>
internal static class CancellationClassifier
{
    /// <summary>
    /// True only when <paramref name="ex"/> represents a cooperative host shutdown (an
    /// <see cref="System.OperationCanceledException"/> raised while <paramref name="stoppingToken"/> is
    /// cancelled). An HttpClient timeout — same exception type, but our token is still live — returns false
    /// so callers treat it as a per-call failure to log and continue past, not a reason to stop.
    /// </summary>
    public static bool IsHostStopping(System.Exception ex, System.Threading.CancellationToken stoppingToken) =>
        ex is System.OperationCanceledException && stoppingToken.IsCancellationRequested;
}
