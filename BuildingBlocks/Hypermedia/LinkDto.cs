using System.Text.Json.Serialization;

namespace BuildingBlocks.Hypermedia;

public sealed record LinkDto(
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("method")] string Method);
