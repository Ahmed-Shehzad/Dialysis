using System.Text.Json.Serialization;

namespace Dialysis.Module.Contracts.Hateoas;

public sealed record LinkDto(
    [property: JsonPropertyName("rel")] string Rel,
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("method")] string Method);
