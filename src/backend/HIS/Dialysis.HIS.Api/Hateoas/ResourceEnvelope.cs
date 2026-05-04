using System.Text.Json.Serialization;

namespace Dialysis.HIS.Api.Hateoas;

public sealed record ResourceEnvelope<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("links")] IReadOnlyList<LinkDto> Links);
