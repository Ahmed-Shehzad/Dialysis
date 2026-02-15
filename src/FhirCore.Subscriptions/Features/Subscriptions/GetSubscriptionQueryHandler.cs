using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed class GetSubscriptionQueryHandler : IQueryHandler<GetSubscriptionQuery, SubscriptionEntry?>
{
    private readonly ISubscriptionsStore _store;

    public GetSubscriptionQueryHandler(ISubscriptionsStore store) => _store = store;

    public Task<SubscriptionEntry?> HandleAsync(GetSubscriptionQuery request, CancellationToken cancellationToken = default)
        => _store.GetByIdAsync(request.Id, cancellationToken);
}
