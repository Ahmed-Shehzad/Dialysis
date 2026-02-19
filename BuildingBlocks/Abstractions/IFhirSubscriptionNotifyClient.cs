namespace BuildingBlocks.Abstractions;

/// <summary>
/// Client for notifying the FHIR subscription dispatcher when resources are created or updated.
/// Used by Treatment and Alarm services to trigger rest-hook callbacks.
/// </summary>
public interface IFhirSubscriptionNotifyClient
{
    Task NotifyAsync(
        string resourceType,
        string resourceUrl,
        string? tenantId,
        string? authorization,
        CancellationToken cancellationToken = default);
}
