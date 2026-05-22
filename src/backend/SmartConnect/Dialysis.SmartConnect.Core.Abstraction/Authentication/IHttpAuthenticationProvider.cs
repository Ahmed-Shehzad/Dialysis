namespace Dialysis.SmartConnect.Authentication;

/// <summary>
/// Applies an authentication scheme (Bearer, API key, Basic, OAuth2 client-credentials, …) to an outbound
/// HTTP request right before it is sent. Implementations are matched per outbound route via
/// <see cref="Kind"/>; the runtime resolves them through <see cref="IHttpAuthenticationProviderRegistry"/>.
/// </summary>
/// <remarks>
/// Mirth Connect's HTTP destination authentication mode (User Guide pp. 245–252) is the reference shape.
/// SmartConnect's generic <c>HttpOutboundAdapter</c> calls <see cref="ApplyAsync"/> after building the
/// request and before <c>HttpClient.SendAsync</c>, so providers can attach headers (Authorization,
/// X-API-Key, custom schemes) or any other request-level concern without forcing every flow author to
/// hand-script auth in a JavaScript transform.
/// </remarks>
public interface IHttpAuthenticationProvider
{
    /// <summary>
    /// Stable string identifier matched against the <c>Kind</c> on the per-route authentication
    /// parameters JSON. Convention: lower-kebab-case (<c>bearer</c>, <c>api-key</c>, <c>basic</c>,
    /// <c>oauth2-client-credentials</c>).
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Mutates the request to carry the credentials produced by this provider. The
    /// <paramref name="parametersJson"/> argument is the raw JSON object from the route's
    /// <c>Authentication</c> property; providers parse it into their own option type. Returning
    /// without throwing means "go ahead and send".
    /// </summary>
    Task ApplyAsync(
        HttpRequestMessage request,
        string parametersJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Slice A2: lets a provider swap the <see cref="HttpClient"/> used to send the request
    /// when the auth scheme is bound to the underlying handler rather than the request — the
    /// canonical case is mutual TLS, where the client certificate lives on the
    /// <see cref="System.Net.Http.SocketsHttpHandler"/>'s SSL options and can't be attached
    /// per-request. Returning <c>null</c> tells the adapter to keep using its default
    /// <c>smartconnect-outbound</c> named client. Default implementation is a no-op so the
    /// four header-based providers (Bearer / API key / Basic / OAuth2) don't need to change.
    /// </summary>
    Task<HttpClient?> ResolveClientAsync(
        string parametersJson,
        HttpClient defaultClient,
        CancellationToken cancellationToken) =>
        Task.FromResult<HttpClient?>(null);
}
