using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed record CreateSubscriptionCommand(string Criteria, string Endpoint, string? EndpointType) : ICommand<SubscriptionEntry>;
