using System.Net.Http.Headers;
using Dialysis.ApiClients;
using Dialysis.PublicHealth.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.PublicHealth.Services;

/// <summary>Delivers reports via Refit IReportDeliveryApi.</summary>
public sealed class RefitReportDeliveryService : IReportDeliveryService
{
    private readonly IReportDeliveryApi _api;
    private readonly PublicHealthOptions _options;

    public RefitReportDeliveryService(IReportDeliveryApi api, IOptions<PublicHealthOptions> options)
    {
        _api = api;
        _options = options.Value;
    }

    public async Task<DeliveryResult> DeliverAsync(Stream content, string contentType, string? filename, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ReportDeliveryEndpoint))
            return new DeliveryResult(true, null);

        try
        {
            var contentDisposition = !string.IsNullOrEmpty(filename)
                ? new ContentDispositionHeaderValue("attachment") { FileName = filename }.ToString()
                : null;
            await _api.DeliverAsync(content, contentType, contentDisposition, cancellationToken);
            return new DeliveryResult(true, null);
        }
        catch (Exception ex)
        {
            return new DeliveryResult(false, ex.Message);
        }
    }
}
