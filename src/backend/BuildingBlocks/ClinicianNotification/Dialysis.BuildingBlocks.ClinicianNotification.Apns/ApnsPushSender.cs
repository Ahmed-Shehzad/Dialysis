using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Apns;

/// <summary>
/// Apple Push Notification service sender using JWT-based authentication. Signs an
/// ES256 JWT with the operator-supplied <c>.p8</c> private key on every request (Apple
/// requires re-signing every 20–60 minutes anyway; we re-sign per send to keep the code
/// simple and stateless). Posts to <c>https://api.push.apple.com/3/device/{token}</c>
/// (or the sandbox host when <see cref="ApnsPushOptions.UseSandbox"/> is set).
///
/// PHI minimisation: same as the other senders — the body comes pre-shaped from the
/// dispatcher.
/// </summary>
public sealed class ApnsPushSender : IClinicianNotificationSender
{
    private const string ProdHost = "https://api.push.apple.com";
    private const string SandboxHost = "https://api.sandbox.push.apple.com";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApnsPushOptions _options;
    private readonly ILogger<ApnsPushSender> _logger;

    public ApnsPushSender(
        IHttpClientFactory httpClientFactory,
        IOptions<ApnsPushOptions> options,
        ILogger<ApnsPushSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ChannelCode => "push.apns";

    public async Task<ClinicianNotificationResult> SendAsync(
        ClinicianNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            return new ClinicianNotificationResult(false, null, "APNs not configured.");
        }

        try
        {
            var jwt = SignJwt();
            var host = _options.UseSandbox ? SandboxHost : ProdHost;
            using var client = _httpClientFactory.CreateClient("clinician-notification-apns");
            client.DefaultRequestVersion = new Version(2, 0);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", jwt);

            var payload = new
            {
                aps = new
                {
                    alert = new { title = request.Subject, body = request.Body },
                    sound = request.Priority == NotificationPriority.Critical ? "default" : null,
                },
                metadata = request.Metadata,
            };
            using var content = JsonContent.Create(payload);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{host}/3/device/{request.Address}")
            {
                Content = content,
                Version = new Version(2, 0),
            };
            httpRequest.Headers.Add("apns-topic", _options.BundleId);
            httpRequest.Headers.Add("apns-push-type", "alert");
            httpRequest.Headers.Add("apns-priority", request.Priority == NotificationPriority.Critical ? "10" : "5");

            using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new ClinicianNotificationResult(false, null, $"APNs HTTP {(int)response.StatusCode}: {Truncate(body)}");
            }
            var messageId = response.Headers.TryGetValues("apns-id", out var ids) ? ids.FirstOrDefault() : null;
            return new ClinicianNotificationResult(true, messageId, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "APNs POST failed for {Address}.", request.Address);
            return new ClinicianNotificationResult(false, null, ex.GetType().Name);
        }
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.TeamId)
        && !string.IsNullOrWhiteSpace(_options.KeyId)
        && !string.IsNullOrWhiteSpace(_options.BundleId)
        && !string.IsNullOrWhiteSpace(_options.PrivateKeyPem);

    /// <summary>
    /// Builds + signs an ES256 JWT per Apple's APNs requirements. Header carries the
    /// key id; payload carries the team id and a fresh <c>iat</c>.
    /// </summary>
    private string SignJwt()
    {
        var header = JsonSerializer.Serialize(new { alg = "ES256", kid = _options.KeyId });
        var payload = JsonSerializer.Serialize(new
        {
            iss = _options.TeamId,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        });
        var signingInput = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(header))}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}";

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(_options.PrivateKeyPem);
        var signature = ecdsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256);
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Truncate(string s) => s.Length <= 512 ? s : s[..512];
}

public sealed class ApnsPushOptions
{
    /// <summary>Apple Developer Team ID — 10 chars.</summary>
    public string? TeamId { get; set; }

    /// <summary>APNs auth key ID — 10 chars; matches the <c>.p8</c> filename.</summary>
    public string? KeyId { get; set; }

    /// <summary>Application bundle id (the <c>apns-topic</c> header).</summary>
    public string? BundleId { get; set; }

    /// <summary>The PEM-encoded ES256 private key from the operator's APNs auth key (<c>.p8</c>) file.</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Posts to the sandbox host when true; defaults to the production host.</summary>
    public bool UseSandbox { get; set; }
}
