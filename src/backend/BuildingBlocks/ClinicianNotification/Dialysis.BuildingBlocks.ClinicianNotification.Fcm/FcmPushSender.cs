using System.Net.Http.Headers;
using System.Net.Http.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Fcm;

/// <summary>
/// Firebase Cloud Messaging push sender (HTTP v1 API,
/// <c>https://fcm.googleapis.com/v1/projects/{projectId}/messages:send</c>).
/// Authenticates with a Google service-account JSON via
/// <see cref="GoogleCredential"/>; access tokens are cached + auto-refreshed by the
/// underlying library so we don't have to manage the OAuth round-trip ourselves.
///
/// PHI minimisation: the push body is the dispatcher-provided string; we do not template
/// here. The destination is the FCM device-registration token of the clinician's mobile
/// app session, distinct per-device.
/// </summary>
public sealed class FcmPushSender : IClinicianNotificationSender
{
    private static readonly string[] _scopes = ["https://www.googleapis.com/auth/firebase.messaging"];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FcmPushOptions _options;
    private readonly ILogger<FcmPushSender> _logger;
    private GoogleCredential? _credential;

    public FcmPushSender(
        IHttpClientFactory httpClientFactory,
        IOptions<FcmPushOptions> options,
        ILogger<FcmPushSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ChannelCode => "push.fcm";

    public async Task<ClinicianNotificationResult> SendAsync(
        ClinicianNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            return new ClinicianNotificationResult(false, null, "FCM not configured.");
        }

        try
        {
            var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

            using var client = _httpClientFactory.CreateClient("clinician-notification-fcm");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                message = new
                {
                    token = request.Address,
                    notification = new { title = request.Subject, body = request.Body },
                    data = request.Metadata,
                    android = new { priority = request.Priority == NotificationPriority.Critical ? "high" : "normal" },
                },
            };
            var url = $"https://fcm.googleapis.com/v1/projects/{_options.ProjectId}/messages:send";
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new ClinicianNotificationResult(false, null, $"FCM HTTP {(int)response.StatusCode}: {Truncate(body)}");
            }
            return new ClinicianNotificationResult(true, null, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM POST failed for {Address}.", request.Address);
            return new ClinicianNotificationResult(false, null, ex.GetType().Name);
        }
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.ProjectId)
        && !string.IsNullOrWhiteSpace(_options.ServiceAccountJson);

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        _credential ??= CredentialFactory.FromJson<ServiceAccountCredential>(_options.ServiceAccountJson!).ToGoogleCredential().CreateScoped(_scopes);
        return await _credential.UnderlyingCredential
            .GetAccessTokenForRequestAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static string Truncate(string s) => s.Length <= 512 ? s : s[..512];
}

public sealed class FcmPushOptions
{
    /// <summary>Firebase project id — the path segment in the v1 endpoint URL.</summary>
    public string? ProjectId { get; set; }

    /// <summary>Service-account JSON string. Production deployments supply this via a secrets vault, not config files.</summary>
    public string? ServiceAccountJson { get; set; }
}
