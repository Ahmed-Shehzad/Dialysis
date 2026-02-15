using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed record ListSubscriptionsQuery : IQuery<IReadOnlyList<SubscriptionEntry>>;
