using System.Text.Json.Serialization;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Smart;

public sealed record GetSmartConfigurationQuery(string BaseUrl) : IQuery<SmartConfigurationDto>;

public sealed record SmartConfigurationDto(
    [property: JsonPropertyName("authorization_endpoint")] string AuthorizationEndpoint,
    [property: JsonPropertyName("token_endpoint")] string TokenEndpoint,
    [property: JsonPropertyName("capabilities")] string[] Capabilities,
    [property: JsonPropertyName("scopes_supported")] string[] ScopesSupported,
    [property: JsonPropertyName("response_types_supported")] string[] ResponseTypesSupported,
    [property: JsonPropertyName("token_endpoint_auth_methods_supported")] string[] TokenEndpointAuthMethodsSupported,
    [property: JsonPropertyName("code_challenge_methods_supported")] string[] CodeChallengeMethodsSupported);
