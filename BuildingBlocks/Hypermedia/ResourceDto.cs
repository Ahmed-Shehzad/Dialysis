using System.Text.Json.Serialization;

namespace BuildingBlocks.Hypermedia;

public sealed record ResourceDto<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, LinkDto> Links);
