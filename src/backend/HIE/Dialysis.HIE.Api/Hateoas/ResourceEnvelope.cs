using System.Text.Json.Serialization;

namespace Dialysis.HIE.Api.Hateoas;

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

public sealed record LinkDto
{
    public LinkDto(string Rel,
        string Href,
        string Method)
    {
        this.Rel = Rel;
        this.Href = Href;
        this.Method = Method;
    }
    [JsonPropertyName("rel")] public string Rel { get; init; }
    [JsonPropertyName("href")] public string Href { get; init; }
    [JsonPropertyName("method")] public string Method { get; init; }
    public void Deconstruct(out string Rel, out string Href, out string Method)
    {
        Rel = this.Rel;
        Href = this.Href;
        Method = this.Method;
    }
}
