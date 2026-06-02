using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Twilio;

/// <summary>
/// Twilio Programmable Messaging SMS sender. Talks the raw REST API
/// (<c>https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json</c>) so we don't
/// pull the full Twilio .NET SDK and its transitive dependencies — the one POST per
/// outbound message is straightforward enough to model directly.
///
/// Configuration via <see cref="TwilioSmsOptions"/>:
/// <list type="bullet">
///   <item><c>AccountSid</c> — your Twilio account SID (starts with <c>AC</c>).</item>
///   <item><c>AuthToken</c> — REST auth token; never check it into a repo. Production
///         deployments source from Azure Key Vault / AWS Secrets Manager / Vault via
///         the existing secrets-provider chain.</item>
///   <item><c>FromNumber</c> — E.164-formatted sender number provisioned in Twilio.</item>
/// </list>
///
/// PHI minimisation: the SMS body comes pre-shaped from the on-call dispatcher and is
/// already PHI-minimised (chair number + alarm code, no patient name / MRN). We do not
/// re-template here; senders are transport adapters, not content authors.
/// </summary>
public sealed class TwilioSmsSender : IClinicianNotificationSender
{
    private const string ApiBase = "https://api.twilio.com/2010-04-01";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwilioSmsOptions _options;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(
        IHttpClientFactory httpClientFactory,
        IOptions<TwilioSmsOptions> options,
        ILogger<TwilioSmsSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ChannelCode => "sms";

    public async Task<ClinicianNotificationResult> SendAsync(
        ClinicianNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            return new ClinicianNotificationResult(false, null, "Twilio not configured.");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("clinician-notification-twilio");
            client.BaseAddress = new Uri(ApiBase + "/");
            client.DefaultRequestHeaders.Authorization = BuildBasicAuth(_options.AccountSid!, _options.AuthToken!);

            using var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = _options.FromNumber!,
                ["To"] = request.Address,
                ["Body"] = request.Body,
            });
            using var response = await client.PostAsync(
                $"Accounts/{_options.AccountSid}/Messages.json",
                body,
                cancellationToken).ConfigureAwait(false);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new ClinicianNotificationResult(false, null, $"Twilio HTTP {(int)response.StatusCode}: {Truncate(raw)}");
            }

            string? sid = null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("sid", out var sidEl))
                    sid = sidEl.GetString();
            }
            catch (JsonException) { /* SID is informational only — return success even if parse fails. */ }

            return new ClinicianNotificationResult(true, sid, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Twilio SMS POST failed for {Address}.", request.Address);
            return new ClinicianNotificationResult(false, null, ex.GetType().Name);
        }
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.AccountSid)
        && !string.IsNullOrWhiteSpace(_options.AuthToken)
        && !string.IsNullOrWhiteSpace(_options.FromNumber);

    private static AuthenticationHeaderValue BuildBasicAuth(string sid, string token)
    {
        var raw = $"{sid}:{token}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    private static string Truncate(string s) => s.Length <= 256 ? s : s[..256];
}

public sealed class TwilioSmsOptions
{
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? FromNumber { get; set; }
}
