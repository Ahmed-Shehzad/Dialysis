using Microsoft.AspNetCore.Http;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Validates webhook signatures for incoming requests.
/// </summary>
public sealed class WebhookSignatureValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebhookSignatureValidationOptions _options;

    public WebhookSignatureValidationMiddleware(
        RequestDelegate next,
        WebhookSignatureValidationOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.ShouldValidateRequest(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!TryGetHeader(context, _options.SignatureOptions.SignatureHeaderName, out var signature) ||
            !TryGetHeader(context, _options.SignatureOptions.TimestampHeaderName, out var timestamp))
        {
            if (_options.RequireSignature)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context).ConfigureAwait(false);
            return;
        }

        // At this point, signature and timestamp are guaranteed to be non-null due to TryGetHeader check
        if (!long.TryParse(timestamp!, out var unixSeconds))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var sentAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if (_options.TimestampTolerance.HasValue)
        {
            var tolerance = _options.TimestampTolerance.Value;
            var drift = DateTimeOffset.UtcNow - sentAt;
            if (drift.Duration() > tolerance)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        var secret = _options.ResolveSecret(context);
        if (string.IsNullOrWhiteSpace(secret))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await ReadBodyAsync(context).ConfigureAwait(false);
        if (!WebhookSignature.Verify(signature!, secret, timestamp!, body, _options.SignatureOptions))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool TryGetHeader(HttpContext context, string headerName, out string? value)
    {
        value = null;
        if (!context.Request.Headers.TryGetValue(headerName, out var values)) return false;
        value = values.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(value);
    }

    private async static Task<byte[]> ReadBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;

        using var memory = new MemoryStream();
        await context.Request.Body.CopyToAsync(memory).ConfigureAwait(false);
        context.Request.Body.Position = 0;
        return memory.ToArray();
    }

}
