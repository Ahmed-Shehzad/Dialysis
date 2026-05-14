namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Resolves the body of an externally-stored JavaScript file referenced by URI
/// (Mirth UG p279/p283 "External Script Filter Rule" / "External Script Transformer Step").
/// Implementations are responsible for scheme allow-listing, base-path containment,
/// and (where appropriate) TTL caching of the fetched body.
/// </summary>
public interface IExternalScriptLoader
{
    Task<string> LoadAsync(Uri uri, TimeSpan? cacheTtl, CancellationToken cancellationToken);
}
