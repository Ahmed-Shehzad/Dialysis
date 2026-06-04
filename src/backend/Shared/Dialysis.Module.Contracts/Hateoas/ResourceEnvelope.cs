using System.Text.Json.Serialization;

namespace Dialysis.Module.Contracts.Hateoas;

public sealed record ResourceEnvelope<T>
{
    public ResourceEnvelope(T Data,
        IReadOnlyList<LinkDto> Links)
    {
        this.Data = Data;
        this.Links = Links;
    }
    [JsonPropertyName("data")] public T Data { get; init; }
    [JsonPropertyName("links")] public IReadOnlyList<LinkDto> Links { get; init; }
    public void Deconstruct(out T Data, out IReadOnlyList<LinkDto> Links)
    {
        Data = this.Data;
        Links = this.Links;
    }
}
