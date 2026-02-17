using Dialysis.Gateway.Infrastructure;
using Dialysis.Gateway.Services;

using Intercessor.Abstractions;

using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Features.Smart;

public sealed class SmartTokenCommandHandler : ICommandHandler<SmartTokenCommand, SmartTokenResult>
{
    private readonly SmartServerOptions _options;
    private readonly IAuthorizationCodeStore _codeStore;
    private readonly ISmartJwtIssuer _jwtIssuer;

    public SmartTokenCommandHandler(
        IOptions<SmartServerOptions> options,
        IAuthorizationCodeStore codeStore,
        ISmartJwtIssuer jwtIssuer)
    {
        _options = options.Value;
        _codeStore = codeStore;
        _jwtIssuer = jwtIssuer;
    }

    public Task<SmartTokenResult> HandleAsync(SmartTokenCommand request, CancellationToken cancellationToken = default)
    {
        if (request.GrantType != "authorization_code")
            return Task.FromResult(SmartTokenResult.UnsupportedGrantType());

        if (string.IsNullOrWhiteSpace(request.Code))
            return Task.FromResult(SmartTokenResult.InvalidRequest("code is required."));

        var stored = _codeStore.Consume(request.Code);
        if (stored is null)
            return Task.FromResult(SmartTokenResult.InvalidGrant("Invalid or expired authorization code."));

        if (request.RedirectUri != stored.RedirectUri)
            return Task.FromResult(SmartTokenResult.InvalidGrant("redirect_uri mismatch."));

        if (_options.ClientId is not null && (request.ClientId != _options.ClientId || request.ClientSecret != _options.ClientSecret))
            return Task.FromResult(SmartTokenResult.InvalidClient());

        if (!_options.IsConfigured)
            return Task.FromResult(SmartTokenResult.ServerError("SMART server not configured. Set Smart:BaseUrl and Smart:SigningKey."));

        var accessToken = _jwtIssuer.CreateAccessToken(stored.ClientId, stored.Scope, stored.TenantId);
        var expiresIn = 3600;

        return Task.FromResult(SmartTokenResult.Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = expiresIn,
            scope = stored.Scope
        }));
    }
}
