using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for pushing reports to a configured PH endpoint. Base address = ReportDeliveryEndpoint.</summary>
public interface IReportDeliveryApi
{
    [Post("")]
    Task DeliverAsync(
        [Body] Stream content,
        [Header("Content-Type")] string contentType,
        [Header("Content-Disposition")] string? contentDisposition,
        CancellationToken cancellationToken = default);
}
