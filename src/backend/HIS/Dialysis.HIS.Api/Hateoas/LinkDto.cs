using System.Text.Json.Serialization;

namespace Dialysis.HIS.Api.Hateoas;

public sealed record LinkDto(
    [property: JsonPropertyName("rel")] string Rel,
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("method")] string Method);
