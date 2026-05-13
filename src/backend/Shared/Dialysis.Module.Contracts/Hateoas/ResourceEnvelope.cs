using System.Text.Json.Serialization;

namespace Dialysis.Module.Contracts.Hateoas;

public sealed record ResourceEnvelope<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("links")] IReadOnlyList<LinkDto> Links);
