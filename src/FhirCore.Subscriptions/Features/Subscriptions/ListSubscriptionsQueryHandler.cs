using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed class ListSubscriptionsQueryHandler : IQueryHandler<ListSubscriptionsQuery, IReadOnlyList<SubscriptionEntry>>
{
    private readonly ISubscriptionsStore _store;

    public ListSubscriptionsQueryHandler(ISubscriptionsStore store) => _store = store;

    public Task<IReadOnlyList<SubscriptionEntry>> HandleAsync(ListSubscriptionsQuery request, CancellationToken cancellationToken = default)
        => _store.GetAllAsync(cancellationToken);
}
