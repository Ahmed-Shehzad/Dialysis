using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Inbound.AspNetCore;

/// <summary>
/// Maps SmartConnect inbound HTTP routes: <c>POST /smartconnect/v1/flows/{{flowId}}/messages</c>.
/// Optional headers: <c>X-SmartConnect-Correlation-Id</c>, <c>X-SmartConnect-Payload-Format</c> (enum names),
/// <c>X-SmartConnect-ApiKey</c> when configured.
/// </summary>
public static class SmartConnectInboundEndpointExtensions
{
    public const string CorrelationIdHeaderName = "X-SmartConnect-Correlation-Id";

    public const string ApiKeyHeaderName = "X-SmartConnect-ApiKey";

    /// <summary>
    /// Registers POST <c>/smartconnect/v1/flows/{flowId}/messages</c> on the route builder.
    /// </summary>
    public static IEndpointConventionBuilder MapSmartConnectInboundRoutes(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/smartconnect/v1/flows/{flowId:guid}/messages",
            PostInboundMessageAsync).DisableAntiforgery();
    }

    private static async Task PostInboundMessageAsync(
        HttpContext http,
        Guid flowId,
        IInboundMessageFactory messageFactory,
        IInboundTransport inboundTransport,
        IOptions<SmartConnectInboundHttpOptions> options,
        CancellationToken cancellationToken)
    {
        var opts = options.Value;
        if (!string.IsNullOrEmpty(opts.ApiKey))
        {
            if (!http.Request.Headers.TryGetValue(ApiKeyHeaderName, out var key) ||
                !string.Equals(key.ToString(), opts.ApiKey, StringComparison.Ordinal))
            {
                http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        http.Request.Headers.TryGetValue(InboundPayloadFormatResolver.PayloadFormatHeaderName, out var fmtHeader);
        http.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var corrHeader);
        var contentType = http.Request.ContentType;
        var format = InboundPayloadFormatResolver.Resolve(fmtHeader.ToString(), contentType);

        await using var ms = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        while (true)
        {
            var read = await http.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
            if (total > opts.MaxRequestBodyBytes)
            {
                http.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        var payload = ms.ToArray();

        ImmutableDictionary<string, string> metadata = ImmutableDictionary<string, string>.Empty;
        foreach (var header in http.Request.Headers)
        {
            if (header.Key.StartsWith("X-SmartConnect-", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals(InboundPayloadFormatResolver.PayloadFormatHeaderName, StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals(CorrelationIdHeaderName, StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals(ApiKeyHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                metadata = metadata.SetItem(header.Key, header.Value.ToString());
            }
        }

        var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in http.Request.Headers)
        {
            headerMap[header.Key] = header.Value.ToString();
        }

        var queryMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var q in http.Request.Query)
        {
            queryMap[q.Key] = q.Value.ToString();
        }

        var sourceMap = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["httpMethod"] = http.Request.Method,
            ["httpPath"] = http.Request.Path.Value ?? string.Empty,
            ["httpContentType"] = contentType ?? string.Empty,
            ["httpHeaders"] = headerMap,
            ["httpQuery"] = queryMap,
        };
        metadata = metadata.SetItem("smartconnect.sourcemap.json", JsonSerializer.Serialize(sourceMap));

        var message = messageFactory.Create(
            flowId,
            payload,
            format,
            corrHeader.ToString(),
            metadata.Count > 0 ? metadata : null);

        var result = await inboundTransport.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
        http.Response.StatusCode = result.SuggestedHttpStatus;
        if (!result.Succeeded && !string.IsNullOrEmpty(result.Error))
        {
            http.Response.ContentType = "text/plain; charset=utf-8";
            await http.Response.WriteAsync(result.Error, cancellationToken).ConfigureAwait(false);
        }
    }
}
