using Microsoft.AspNetCore.Builder;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Middleware registration helpers for webhook signature validation.
/// </summary>
public static class WebhookSignatureValidationExtensions
{
    public static IApplicationBuilder UseTransponderWebhookSignatureValidation(
        this IApplicationBuilder app,
        Action<WebhookSignatureValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = new WebhookSignatureValidationOptions();
        configure?.Invoke(options);

        return app.UseMiddleware<WebhookSignatureValidationMiddleware>(options);
    }
}
