using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Senders;

/// <summary>
/// HTTP-webhook sender — POSTs the notification JSON to a configured endpoint. Every facility
/// has at least HTTPS egress, so this is the platform's always-on fallback channel: operators
/// can wire it to a paging service, a Slack / Teams incoming webhook, an in-house SMS gateway,
/// or any other webhook-compatible receiver without bringing in a heavy vendor SDK.
///
/// Channel-code is fixed to <c>"webhook"</c> at composition. A single facility can register
/// multiple webhook senders against multiple targets by adding distinct <see cref="WebhookSenderOptions"/>
/// instances via keyed registration.
/// </summary>
public sealed class WebhookClinicianNotificationSender(
    IHttpClientFactory httpClientFactory,
    IOptions<WebhookSenderOptions> options,
    ILogger<WebhookClinicianNotificationSender> logger) : IClinicianNotificationSender
{
    private readonly WebhookSenderOptions _options = options.Value;

    public string ChannelCode => "webhook";

    public async Task<ClinicianNotificationResult> SendAsync(
        ClinicianNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Url))
        {
            return new ClinicianNotificationResult(false, null, "Webhook URL not configured.");
        }

        try
        {
            using var client = httpClientFactory.CreateClient("clinician-notification-webhook");
            using var response = await client.PostAsJsonAsync(_options.Url, new
            {
                channel = request.Channel,
                address = request.Address,
                subject = request.Subject,
                body = request.Body,
                deepLink = request.DeepLink,
                priority = request.Priority.ToString(),
                metadata = request.Metadata,
            }, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new ClinicianNotificationResult(true, response.Headers.TryGetValues("X-Message-Id", out var ids) ? ids.FirstOrDefault() : null, null);
            }
            return new ClinicianNotificationResult(false, null, $"HTTP {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Clinician-notification webhook POST failed.");
            return new ClinicianNotificationResult(false, null, ex.GetType().Name);
        }
    }
}

public sealed class WebhookSenderOptions
{
    public string? Url { get; set; }
}
