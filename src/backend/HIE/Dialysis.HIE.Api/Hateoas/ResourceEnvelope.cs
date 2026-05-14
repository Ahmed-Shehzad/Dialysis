using System.Text.Json.Serialization;

namespace Dialysis.HIE.Api.Hateoas;

public sealed record ResourceEnvelope<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("links")] IReadOnlyList<LinkDto> Links);

public sealed record LinkDto(
    [property: JsonPropertyName("rel")] string Rel,
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("method")] string Method);
