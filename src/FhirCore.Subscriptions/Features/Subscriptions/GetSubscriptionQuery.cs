using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed record GetSubscriptionQuery(string Id) : IQuery<SubscriptionEntry?>;
