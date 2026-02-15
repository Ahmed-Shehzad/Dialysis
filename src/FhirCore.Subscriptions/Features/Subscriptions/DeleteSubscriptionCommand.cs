using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed record DeleteSubscriptionCommand(string Id) : ICommand<bool>;
