using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Options for webhook signature validation middleware.
/// </summary>
public sealed class WebhookSignatureValidationOptions
{
    public WebhookSignatureOptions SignatureOptions { get; } = new();

    public bool RequireSignature { get; set; } = true;

    public TimeSpan? TimestampTolerance { get; set; } = TimeSpan.FromMinutes(5);

    public string? DefaultSecret { get; set; }

    public IDictionary<string, string> SubscriptionSecrets { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public Func<HttpContext, string?>? SubscriptionResolver { get; set; }

    public Func<HttpContext, string?>? SecretResolver { get; set; }

    public Func<HttpContext, bool>? ShouldValidate { get; set; }

    internal string? ResolveSecret(HttpContext context)
    {
        if (SecretResolver is not null) return SecretResolver(context);

        string? subscription = SubscriptionResolver?.Invoke(context);
        if (string.IsNullOrWhiteSpace(subscription))
            if (context.Request.Headers.TryGetValue(TransponderWebhookHeaders.Subscription, out StringValues values))
                subscription = values.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(subscription) &&
            SubscriptionSecrets.TryGetValue(subscription, out string? secret)) return secret;

        return DefaultSecret;
    }

    internal bool ShouldValidateRequest(HttpContext context)
        => ShouldValidate?.Invoke(context) ?? HttpMethods.IsPost(context.Request.Method);
}
