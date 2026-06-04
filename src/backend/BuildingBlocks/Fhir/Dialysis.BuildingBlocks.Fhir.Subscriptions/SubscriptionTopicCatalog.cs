namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// Per-host catalog of supported <c>SubscriptionTopic</c> URLs. Modules register topics at
/// composition time; the catalog backs <c>GET /fhir/SubscriptionTopic</c> discovery and the
/// matcher uses it to reject unknown topics on <c>POST /fhir/Subscription</c>.
/// </summary>
public sealed class SubscriptionTopicCatalog
{
    private readonly Dictionary<string, SubscriptionTopicDescriptor> _topics = new(StringComparer.Ordinal);

    public IReadOnlyCollection<SubscriptionTopicDescriptor> Topics => _topics.Values;

    public bool TryGet(string url, out SubscriptionTopicDescriptor descriptor)
        => _topics.TryGetValue(url, out descriptor!);

    /// <summary>
    /// Registers a topic. Idempotent — re-registering with the same URL overwrites the descriptor.
    /// </summary>
    public SubscriptionTopicCatalog Add(SubscriptionTopicDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _topics[descriptor.Url] = descriptor;
        return this;
    }
}

/// <summary>
/// Topic metadata: canonical URL, human-readable title, description, and the set of filter
/// parameter names a subscriber may use when registering interest. Filter values are matched
/// case-sensitively against payload attributes by <see cref="ISubscriptionMatcher"/>.
/// </summary>
public sealed record SubscriptionTopicDescriptor
{
    /// <summary>
    /// Topic metadata: canonical URL, human-readable title, description, and the set of filter
    /// parameter names a subscriber may use when registering interest. Filter values are matched
    /// case-sensitively against payload attributes by <see cref="ISubscriptionMatcher"/>.
    /// </summary>
    public SubscriptionTopicDescriptor(string Url,
        string Title,
        string Description,
        IReadOnlyList<string> FilterParameterNames)
    {
        this.Url = Url;
        this.Title = Title;
        this.Description = Description;
        this.FilterParameterNames = FilterParameterNames;
    }
    public string Url { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public IReadOnlyList<string> FilterParameterNames { get; init; }
    public void Deconstruct(out string Url, out string Title, out string Description, out IReadOnlyList<string> FilterParameterNames)
    {
        Url = this.Url;
        Title = this.Title;
        Description = this.Description;
        FilterParameterNames = this.FilterParameterNames;
    }
}
