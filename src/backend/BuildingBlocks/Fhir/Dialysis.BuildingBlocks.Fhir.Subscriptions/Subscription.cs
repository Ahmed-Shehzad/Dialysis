namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public sealed record FhirSubscriptionRegistration(
    string Id,
    string TopicUrl,
    SubscriptionChannelType ChannelType,
    string ChannelEndpoint,
    string? ChannelHeader,
    IReadOnlyDictionary<string, string> FilterParameters,
    SubscriptionStatus Status,
    int ConsecutiveFailures = 0);

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
