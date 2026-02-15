namespace Dialysis.PublicHealth.Services;

/// <summary>Delivers generated reports to configured public health endpoints (HTTP push).</summary>
public interface IReportDeliveryService
{
    /// <summary>Push report content to the configured PH endpoint. No-op when ReportDeliveryEndpoint is not configured.</summary>
    Task<DeliveryResult> DeliverAsync(Stream content, string contentType, string? filename, CancellationToken cancellationToken = default);
}

public sealed record DeliveryResult(bool Success, string? Error);
