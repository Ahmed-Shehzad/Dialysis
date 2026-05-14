namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public interface ISubscriptionRegistry
{
    ValueTask<FhirSubscriptionRegistration> RegisterAsync(FhirSubscriptionRegistration registration, CancellationToken cancellationToken);

    ValueTask<FhirSubscriptionRegistration?> GetAsync(string subscriptionId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<FhirSubscriptionRegistration>> ListActiveForTopicAsync(string topicUrl, CancellationToken cancellationToken);

    ValueTask DeleteAsync(string subscriptionId, CancellationToken cancellationToken);

    ValueTask UpdateStatusAsync(string subscriptionId, SubscriptionStatus status, int consecutiveFailures, CancellationToken cancellationToken);
}

public interface ISubscriptionMatcher
{
    /// <summary>
    /// Given a topic and a payload representing an integration event, returns the set of active
    /// subscriptions whose filter parameters match.
    /// </summary>
    ValueTask<IReadOnlyList<FhirSubscriptionRegistration>> MatchAsync(
        string topicUrl,
        IReadOnlyDictionary<string, string> payloadAttributes,
        CancellationToken cancellationToken);
}

public interface ISubscriptionNotificationDispatcher
{
    ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Hl7.Fhir.Model.Resource? payloadResource,
        CancellationToken cancellationToken);
}
