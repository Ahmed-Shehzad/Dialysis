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
    /// Routing-hint header for the content-based router endpoint
    /// (<c>POST /smartconnect/v1/messages</c>). Operators set this to the HL7 trigger
    /// (e.g. <c>ORU^R01</c>) or FHIR ResourceType so flow subscriptions can match without parsing
    /// the payload.
    /// </summary>
    public const string MessageTypeHeaderName = "X-SmartConnect-Message-Type";

    /// <summary>Optional sender id header used by <c>InboundSubscriptionSlot.SenderId</c> matching.</summary>
    public const string SenderIdHeaderName = "X-SmartConnect-Sender-Id";

    /// <summary>Source-connector kind tag used by the router endpoint when fanning to subscriptions.</summary>
    public const string RouterSourceKind = "http";

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Registers two inbound POST endpoints on the route builder:
        ///  • <c>/smartconnect/v1/flows/{flowId}/messages</c> — direct dispatch to a known flow.
        ///  • <c>/smartconnect/v1/messages</c> — content-based fan-out: the router resolves every
        ///    Started flow whose <c>InboundSubscriptions</c> match the headers + payload, and each
        ///    receives a copy of the message.
        /// </summary>
        public IEndpointConventionBuilder MapSmartConnectInboundRoutes()
        {
            endpoints.MapPost(
                "/smartconnect/v1/messages",
                PostRoutedInboundMessageAsync).DisableAntiforgery();
            return endpoints.MapPost(
                "/smartconnect/v1/flows/{flowId:guid}/messages",
                PostInboundMessageAsync).DisableAntiforgery();
        }
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

    /// <summary>
    /// Content-based router endpoint. Resolves all Started flows whose
    /// <see cref="InboundSubscriptionSlot"/>s match the candidate (by source-connector kind +
    /// message-type pattern + sender id) and dispatches a copy of the message to each. Returns
    /// 404 when no subscriptions match — operators register a subscription on the receiving flow
    /// or post to the flow-specific endpoint instead.
    /// </summary>
    private static async Task PostRoutedInboundMessageAsync(
        HttpContext http,
        IInboundMessageFactory messageFactory,
        IInboundTransport inboundTransport,
        IMessageRouter router,
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
        http.Request.Headers.TryGetValue(MessageTypeHeaderName, out var msgTypeHeader);
        http.Request.Headers.TryGetValue(SenderIdHeaderName, out var senderHeader);

        var contentType = http.Request.ContentType;
        var format = InboundPayloadFormatResolver.Resolve(fmtHeader.ToString(), contentType);

        await using var ms = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        while (true)
        {
            var read = await http.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            total += read;
            if (total > opts.MaxRequestBodyBytes)
            {
                http.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }
            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        var payload = ms.ToArray();
        var messageType = string.IsNullOrWhiteSpace(msgTypeHeader.ToString()) ? null : msgTypeHeader.ToString();
        var senderId = string.IsNullOrWhiteSpace(senderHeader.ToString()) ? null : senderHeader.ToString();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (messageType is not null) metadata["smartconnect.messageType"] = messageType;
        if (senderId is not null) metadata["smartconnect.senderId"] = senderId;

        var candidate = new MessageRoutingCandidate(
            SourceKind: RouterSourceKind,
            MessageType: messageType,
            SenderId: senderId,
            Metadata: metadata);

        var flowIds = await router.ResolveFlowIdsAsync(candidate, cancellationToken).ConfigureAwait(false);
        if (flowIds.Count == 0)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            http.Response.ContentType = "text/plain; charset=utf-8";
            await http.Response.WriteAsync(
                "No flow subscriptions match the supplied headers. Register InboundSubscriptions on a Started flow or POST to /smartconnect/v1/flows/{flowId}/messages instead.",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var dispatchTasks = new List<Task<InboundReceiveResult>>(flowIds.Count);
        foreach (var flowId in flowIds)
        {
            var message = messageFactory.Create(
                flowId,
                payload,
                format,
                corrHeader.ToString(),
                metadata);
            dispatchTasks.Add(inboundTransport.DispatchAsync(message, cancellationToken));
        }

        var results = await Task.WhenAll(dispatchTasks).ConfigureAwait(false);
        var anyFailed = false;
        var aggregateError = new System.Text.StringBuilder();
        foreach (var r in results)
        {
            if (!r.Succeeded)
            {
                anyFailed = true;
                if (!string.IsNullOrEmpty(r.Error))
                {
                    aggregateError.AppendLine(r.Error);
                }
            }
        }

        http.Response.StatusCode = anyFailed
            ? StatusCodes.Status207MultiStatus
            : StatusCodes.Status202Accepted;
        if (anyFailed && aggregateError.Length > 0)
        {
            http.Response.ContentType = "text/plain; charset=utf-8";
            await http.Response.WriteAsync(aggregateError.ToString(), cancellationToken).ConfigureAwait(false);
        }
    }
}
