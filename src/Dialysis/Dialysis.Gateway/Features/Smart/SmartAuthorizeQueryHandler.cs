using System.Web;

using Dialysis.Gateway.Infrastructure;
using Dialysis.Gateway.Services;

using Intercessor.Abstractions;
using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Features.Smart;

public sealed class SmartAuthorizeQueryHandler : IQueryHandler<SmartAuthorizeQuery, SmartAuthorizeResult>
{
    private readonly SmartServerOptions _options;
    private readonly IAuthorizationCodeStore _codeStore;
    private readonly ILogger<SmartAuthorizeQueryHandler> _logger;

    public SmartAuthorizeQueryHandler(
        IOptions<SmartServerOptions> options,
        IAuthorizationCodeStore codeStore,
        ILogger<SmartAuthorizeQueryHandler> logger)
    {
        _options = options.Value;
        _codeStore = codeStore;
        _logger = logger;
    }

    public Task<SmartAuthorizeResult> HandleAsync(SmartAuthorizeQuery request, CancellationToken cancellationToken = default)
    {
        if (request.ResponseType != "code")
            return Task.FromResult(new SmartAuthorizeResult(null, "unsupported_response_type", "Only response_type=code is supported."));

        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.RedirectUri))
            return Task.FromResult(new SmartAuthorizeResult(null, "invalid_request", "client_id and redirect_uri are required."));

        if (_options.ClientId is not null && _options.ClientId != request.ClientId)
        {
            _logger.LogWarning("SMART authorize: client_id {ClientId} not authorized", request.ClientId);
            return Task.FromResult(new SmartAuthorizeResult($"{request.RedirectUri}?error=unauthorized_client&state={HttpUtility.UrlEncode(request.State ?? "")}", null, null));
        }

        var code = Guid.NewGuid().ToString("N");
        _codeStore.Store(code, new StoredAuthorizationCode(request.ClientId, request.RedirectUri, request.Scope ?? "", request.State, request.Tenant ?? "default"), TimeSpan.FromMinutes(5));

        var qs = $"code={HttpUtility.UrlEncode(code)}";
        if (!string.IsNullOrEmpty(request.State))
            qs += $"&state={HttpUtility.UrlEncode(request.State)}";

        var sep = request.RedirectUri.Contains('?') ? "&" : "?";
        return Task.FromResult(new SmartAuthorizeResult($"{request.RedirectUri}{sep}{qs}", null, null));
    }
}
