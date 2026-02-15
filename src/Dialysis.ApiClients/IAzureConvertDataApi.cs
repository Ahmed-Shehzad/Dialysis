using System.Text.Json.Serialization;
using Hl7.Fhir.Model;
using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for Azure FHIR $convert-data operation. Base address = FHIR service URL.</summary>
public interface IAzureConvertDataApi
{
    [Post("$convert-data")]
    Task<Bundle> ConvertAsync([Body] ConvertDataRequest body, CancellationToken cancellationToken = default);
}

public sealed record ConvertDataRequest(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("parameter")] List<ConvertDataParameter> Parameter);

public sealed record ConvertDataParameter(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("valueString")] string? ValueString,
    [property: JsonPropertyName("valueDataType")] string? ValueDataType);
