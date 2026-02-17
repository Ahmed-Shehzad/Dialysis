using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Smart;

public sealed record SmartTokenCommand(
    string GrantType,
    string? Code,
    string? RedirectUri,
    string? ClientId,
    string? ClientSecret,
    string? CodeVerifier) : ICommand<SmartTokenResult>;

public sealed record SmartTokenResult(
    bool Success,
    object? TokenResponse,
    string? Error,
    string? ErrorDescription,
    int StatusCode)
{
    public static SmartTokenResult Ok(object tokenResponse)
        => new(true, tokenResponse, null, null, 200);

    public static SmartTokenResult UnsupportedGrantType()
        => new(false, null, "unsupported_grant_type", null, 400);

    public static SmartTokenResult InvalidRequest(string description)
        => new(false, null, "invalid_request", description, 400);

    public static SmartTokenResult InvalidGrant(string description)
        => new(false, null, "invalid_grant", description, 400);

    public static SmartTokenResult InvalidClient()
        => new(false, null, "invalid_client", null, 400);

    public static SmartTokenResult ServerError(string description)
        => new(false, null, "server_error", description, 500);
}
