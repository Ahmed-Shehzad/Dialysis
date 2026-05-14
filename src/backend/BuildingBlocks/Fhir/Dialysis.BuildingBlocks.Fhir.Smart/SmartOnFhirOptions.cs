namespace Dialysis.BuildingBlocks.Fhir.Smart;

public sealed class SmartOnFhirOptions
{
    public required string Issuer { get; set; }

    public required string AuthorizationEndpoint { get; set; }

    public required string TokenEndpoint { get; set; }

    public string? RevocationEndpoint { get; set; }

    public string? IntrospectionEndpoint { get; set; }

    public string? ManagementEndpoint { get; set; }

    /// <summary>SMART capabilities advertised in <c>.well-known/smart-configuration</c>.</summary>
    public IReadOnlyList<string> Capabilities { get; set; } =
    [
        "launch-ehr",
        "launch-standalone",
        "client-public",
        "client-confidential-symmetric",
        "context-passthrough-ehr-launch",
        "permission-patient",
        "permission-user",
        "permission-offline",
        "sso-openid-connect",
    ];

    public IReadOnlyList<string> ScopesSupported { get; set; } =
    [
        "openid",
        "profile",
        "fhirUser",
        "launch",
        "launch/patient",
        "offline_access",
        "patient/*.read",
        "user/*.read",
        "system/*.read",
    ];

    /// <summary>Maps SMART scope names to <c>IModulePermissionCatalog</c> permission strings.</summary>
    public Dictionary<string, string[]> ScopePermissionMap { get; set; } = new();
}
