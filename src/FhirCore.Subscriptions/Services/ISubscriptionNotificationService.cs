namespace FhirCore.Subscriptions.Services;

public interface ISubscriptionNotificationService
{
    Task OnResourceWrittenAsync(string resourceType, string resourceId, IReadOnlyDictionary<string, string>? searchContext, CancellationToken cancellationToken = default);
}
