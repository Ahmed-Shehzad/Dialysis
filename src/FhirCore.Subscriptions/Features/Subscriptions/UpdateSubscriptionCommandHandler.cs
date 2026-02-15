using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed class UpdateSubscriptionCommandHandler : ICommandHandler<UpdateSubscriptionCommand, bool>
{
    private readonly ISubscriptionsStore _store;

    public UpdateSubscriptionCommandHandler(ISubscriptionsStore store) => _store = store;

    public Task<bool> HandleAsync(UpdateSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        var entry = new SubscriptionEntry
        {
            Id = request.Id,
            Criteria = request.Criteria,
            Endpoint = request.Endpoint,
            EndpointType = request.EndpointType
        };
        return _store.UpdateAsync(request.Id, entry, cancellationToken);
    }
}
