using System.Text.Json.Serialization;

namespace Dialysis.PublicHealth.Configuration;

/// <summary>JSON config for reportable conditions. See docs/reportable-conditions/REPORTABLE-CONDITIONS-REGISTRY.json</summary>
public sealed class ReportableConditionsConfig
{
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("jurisdictions")]
    public IReadOnlyList<JurisdictionConfig>? Jurisdictions { get; init; }

    [JsonPropertyName("conditions")]
    public IReadOnlyList<ConditionConfig>? Conditions { get; init; }
}

public sealed class JurisdictionConfig
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("reportingRequired")]
    public bool ReportingRequired { get; init; }
}

public sealed class ConditionConfig
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("system")]
    public string? System { get; init; }

    [JsonPropertyName("display")]
    public string? Display { get; init; }

    [JsonPropertyName("reportable")]
    public bool Reportable { get; init; } = true;

    [JsonPropertyName("jurisdictions")]
    public IReadOnlyList<string>? Jurisdictions { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }
}
