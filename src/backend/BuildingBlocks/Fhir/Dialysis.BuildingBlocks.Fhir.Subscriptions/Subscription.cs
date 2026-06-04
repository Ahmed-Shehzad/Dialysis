namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public sealed record FhirSubscriptionRegistration
{
    public FhirSubscriptionRegistration(string Id,
        string TopicUrl,
        SubscriptionChannelType ChannelType,
        string ChannelEndpoint,
        string? ChannelHeader,
        IReadOnlyDictionary<string, string> FilterParameters,
        SubscriptionStatus Status,
        int ConsecutiveFailures = 0)
    {
        this.Id = Id;
        this.TopicUrl = TopicUrl;
        this.ChannelType = ChannelType;
        this.ChannelEndpoint = ChannelEndpoint;
        this.ChannelHeader = ChannelHeader;
        this.FilterParameters = FilterParameters;
        this.Status = Status;
        this.ConsecutiveFailures = ConsecutiveFailures;
    }
    public string Id { get; init; }
    public string TopicUrl { get; init; }
    public SubscriptionChannelType ChannelType { get; init; }
    public string ChannelEndpoint { get; init; }
    public string? ChannelHeader { get; init; }
    public IReadOnlyDictionary<string, string> FilterParameters { get; init; }
    public SubscriptionStatus Status { get; init; }
    public int ConsecutiveFailures { get; init; }
    public void Deconstruct(out string Id, out string TopicUrl, out SubscriptionChannelType ChannelType, out string ChannelEndpoint, out string? ChannelHeader, out IReadOnlyDictionary<string, string> FilterParameters, out SubscriptionStatus Status, out int ConsecutiveFailures)
    {
        Id = this.Id;
        TopicUrl = this.TopicUrl;
        ChannelType = this.ChannelType;
        ChannelEndpoint = this.ChannelEndpoint;
        ChannelHeader = this.ChannelHeader;
        FilterParameters = this.FilterParameters;
        Status = this.Status;
        ConsecutiveFailures = this.ConsecutiveFailures;
    }
}

public enum SubscriptionChannelType
{
    RestHook,
    WebSocket,
    ServerSentEvents,
    Email,
    Sms,
}

public enum SubscriptionStatus
{
    Requested,
    Active,
    Error,
    Off,
}
