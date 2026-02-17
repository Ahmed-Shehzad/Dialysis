using Dialysis.Gateway.Infrastructure;

using Intercessor.Abstractions;

using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Features.Smart;

public sealed class GetSmartConfigurationQueryHandler : IQueryHandler<GetSmartConfigurationQuery, SmartConfigurationDto>
{
    private readonly SmartServerOptions _options;

    public GetSmartConfigurationQueryHandler(IOptions<SmartServerOptions> options)
    {
        _options = options.Value;
    }

    public Task<SmartConfigurationDto> HandleAsync(GetSmartConfigurationQuery request, CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrEmpty(_options.BaseUrl)
            ? request.BaseUrl.TrimEnd('/')
            : _options.BaseUrl.TrimEnd('/');

        var dto = new SmartConfigurationDto(
            AuthorizationEndpoint: $"{baseUrl}/auth/authorize",
            TokenEndpoint: $"{baseUrl}/auth/token",
            Capabilities: ["launch-standalone", "client-confidential-symmetric"],
            ScopesSupported: ["openid", "fhirUser", "patient/*.*", "user/*.*"],
            ResponseTypesSupported: ["code"],
            TokenEndpointAuthMethodsSupported: ["client_secret_post", "client_secret_basic"],
            CodeChallengeMethodsSupported: ["S256"]);

        return Task.FromResult(dto);
    }
}
