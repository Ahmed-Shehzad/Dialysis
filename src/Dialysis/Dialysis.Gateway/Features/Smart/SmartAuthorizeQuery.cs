using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Smart;

public sealed record SmartAuthorizeQuery(
    string ClientId,
    string ResponseType,
    string RedirectUri,
    string? Scope,
    string? State,
    string? Launch,
    string? Tenant) : IQuery<SmartAuthorizeResult>;

public sealed record SmartAuthorizeResult(string? RedirectUrl, string? Error, string? ErrorDescription);
