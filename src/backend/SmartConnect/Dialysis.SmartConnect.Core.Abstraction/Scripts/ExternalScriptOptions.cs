namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Bounds what URIs <see cref="IExternalScriptLoader"/> may resolve. Defaults are deliberately
/// closed (no file roots, no http hosts) so operators must opt-in before external scripts can run.
/// </summary>
public sealed class ExternalScriptOptions
{
    /// <summary>Absolute directory paths under which <c>file://</c> scripts may live. Empty list disables file://.</summary>
    public IList<string> AllowedFileRoots { get; set; } = new List<string>();

    /// <summary>Host names allowed for <c>http(s)://</c> scripts (case-insensitive). Empty list disables http(s)://.</summary>
    public IList<string> AllowedHttpHosts { get; set; } = new List<string>();

    /// <summary>Default cache TTL when a plugin slot doesn't specify one. <c>TimeSpan.Zero</c> disables caching.</summary>
    public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Upper bound on the size of any single fetched script body, in bytes. Defaults to 256 KiB.</summary>
    public int MaxScriptBytes { get; set; } = 256 * 1024;
}
