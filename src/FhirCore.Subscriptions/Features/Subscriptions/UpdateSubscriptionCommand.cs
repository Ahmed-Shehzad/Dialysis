using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed record UpdateSubscriptionCommand(string Id, string Criteria, string Endpoint, string? EndpointType) : ICommand<bool>;
