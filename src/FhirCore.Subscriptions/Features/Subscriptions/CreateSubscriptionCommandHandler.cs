using Intercessor.Abstractions;

namespace FhirCore.Subscriptions.Features.Subscriptions;

public sealed class CreateSubscriptionCommandHandler : ICommandHandler<CreateSubscriptionCommand, SubscriptionEntry>
{
    private readonly ISubscriptionsStore _store;

    public CreateSubscriptionCommandHandler(ISubscriptionsStore store) => _store = store;

    public async Task<SubscriptionEntry> HandleAsync(CreateSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var entry = new SubscriptionEntry
        {
            Id = id,
            Criteria = request.Criteria,
            Endpoint = request.Endpoint,
            EndpointType = request.EndpointType
        };
        return await _store.AddAsync(entry, cancellationToken);
    }
}
