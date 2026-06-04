using System.Text.Json.Serialization;

namespace Dialysis.HIS.Api.Hateoas;

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
