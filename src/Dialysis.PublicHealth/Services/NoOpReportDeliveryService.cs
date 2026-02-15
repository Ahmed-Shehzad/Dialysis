namespace Dialysis.PublicHealth.Services;

/// <summary>No-op report delivery when PH endpoint is not configured.</summary>
public sealed class NoOpReportDeliveryService : IReportDeliveryService
{
    public Task<DeliveryResult> DeliverAsync(Stream content, string contentType, string? filename, CancellationToken cancellationToken = default)
        => Task.FromResult(new DeliveryResult(true, null));
}
