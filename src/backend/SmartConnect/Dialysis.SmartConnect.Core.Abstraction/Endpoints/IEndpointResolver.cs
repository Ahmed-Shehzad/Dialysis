namespace Dialysis.SmartConnect.Endpoints;

/// <summary>
/// Resolves an outbound route's parameter JSON. When the JSON parses as
/// <c>{"endpointRef":"some-name"}</c> the resolver looks up the named endpoint and returns its
/// stored <c>ParametersJson</c>; otherwise it passes the value through unchanged so flows that
/// inline their parameters continue to work.
/// </summary>
/// <remarks>
/// Hosts without a persistence-backed resolver wire <c>PassThroughEndpointResolver</c>, which keeps
/// existing inline-JSON flows behaving exactly as before.
/// </remarks>
public interface IEndpointResolver
{
    Task<string?> ResolveParametersJsonAsync(string? nameOrInline, CancellationToken cancellationToken);
}

/// <summary>
/// No-op resolver: returns the input unchanged. Used when the host hasn't registered a database-
/// backed implementation, so flows without <c>endpointRef</c> see no behaviour change.
/// </summary>
public sealed class PassThroughEndpointResolver : IEndpointResolver
{
    public Task<string?> ResolveParametersJsonAsync(string? nameOrInline, CancellationToken cancellationToken) =>
        Task.FromResult(nameOrInline);
}
