using BuildingBlocks.Abstractions;
using Dialysis.Contracts.Events;
using FhirCore.Subscriptions.Services;

namespace FhirCore.Subscriptions;

/// <summary>
/// Handles ResourceWrittenEvent from the message bus and notifies subscribers.
/// </summary>
public sealed class ResourceWrittenIntegrationEventHandler : IIntegrationEventHandler<ResourceWrittenEvent>
{
    private readonly ISubscriptionNotificationService _notificationService;

    public ResourceWrittenIntegrationEventHandler(ISubscriptionNotificationService notificationService)
    {
        _notificationService = notificationService
            ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public Task HandleAsync(ResourceWrittenEvent message, CancellationToken cancellationToken = default)
        => _notificationService.OnResourceWrittenAsync(
            message.ResourceType,
            message.ResourceId,
            message.SearchContext,
            cancellationToken);
}
