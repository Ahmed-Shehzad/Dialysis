using System.Text.Json.Serialization;

namespace BuildingBlocks.Hypermedia;

public sealed record CollectionDto<T>(
    [property: JsonPropertyName("items")] IReadOnlyList<T> Items,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, LinkDto> Links,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null);
