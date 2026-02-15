namespace BuildingBlocks.Hypermedia;

public static class Hateoas
{
    public static ResourceDto<T> Resource<T>(
        T data,
        IReadOnlyDictionary<string, LinkDto> links)
        => new(data, links);

    public static CollectionDto<T> Collection<T>(
        IReadOnlyList<T> items,
        IReadOnlyDictionary<string, LinkDto> links,
        PaginationMeta? meta = null)
        => new(items, links, meta);

    public static CollectionDto<ResourceDto<T>> Collection<T>(
        IReadOnlyList<T> items,
        Func<T, IReadOnlyDictionary<string, LinkDto>> linkFactory,
        IReadOnlyDictionary<string, LinkDto> collectionLinks,
        PaginationMeta? meta = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(linkFactory);

        IReadOnlyList<ResourceDto<T>> resources = [.. items.Select(item => new ResourceDto<T>(item, linkFactory(item)))];
        return new CollectionDto<ResourceDto<T>>(resources, collectionLinks, meta);
    }

    public static IReadOnlyDictionary<string, LinkDto> Links(
        params (string Rel, string Href, string Method)[] links)
    {
        var map = new Dictionary<string, LinkDto>(StringComparer.OrdinalIgnoreCase);

        foreach ((var rel, var href, var method) in links)
        {
            if (string.IsNullOrWhiteSpace(rel)) continue;
            map[rel] = new LinkDto(href ?? string.Empty, method ?? string.Empty);
        }

        return map;
    }

    public static LinkDto Link(string href, string method)
        => new(href ?? string.Empty, method ?? string.Empty);
}
