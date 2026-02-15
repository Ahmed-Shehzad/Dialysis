using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed class DeleteSubscriptionCommandHandler : ICommandHandler<DeleteSubscriptionCommand, bool>
{
    private readonly ISubscriptionsStore _store;

    public DeleteSubscriptionCommandHandler(ISubscriptionsStore store) => _store = store;

    public Task<bool> HandleAsync(DeleteSubscriptionCommand request, CancellationToken cancellationToken = default)
        => _store.RemoveAsync(request.Id, cancellationToken);
}
