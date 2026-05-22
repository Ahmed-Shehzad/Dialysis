namespace Dialysis.SmartConnect.Authentication;

/// <summary>
/// Lookup port for <see cref="IHttpAuthenticationProvider"/>s. The runtime resolves the registry once
/// at host start; outbound adapters call <see cref="TryGet"/> per send. Returns false (rather than
/// throwing) for unknown kinds so adapters can surface a friendly ProblemDetails-style error and
/// continue draining the inbox.
/// </summary>
public interface IHttpAuthenticationProviderRegistry
{
    bool TryGet(string kind, out IHttpAuthenticationProvider provider);
}
