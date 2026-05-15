using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public static class FhirSubscriptionEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the FHIR <c>Subscription</c> management endpoints — the single controlled write
        /// exception to the v1 "no writes on resource routes" rule, since clients must be able to
        /// register interest. Also exposes the per-host <c>SubscriptionTopic</c> catalog.
        /// </summary>
        public IEndpointRouteBuilder MapFhirSubscriptionEndpoints(
            string baseUrl = "/fhir",
            string? requireScope = null)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            var prefix = baseUrl.TrimEnd('/');

            var routes = new List<IEndpointConventionBuilder>
        {
            endpoints.MapPost(prefix + "/Subscription", CreateAsync),
            endpoints.MapGet(prefix + "/Subscription/{id}", GetAsync),
            endpoints.MapDelete(prefix + "/Subscription/{id}", DeleteAsync),
            endpoints.MapGet(prefix + "/SubscriptionTopic", ListTopicsAsync),
            endpoints.MapGet(prefix + "/SubscriptionTopic/{name}", GetTopicAsync),
            endpoints.MapGet(prefix + "/subscription/sse", SseAsync),
            endpoints.MapGet(prefix + "/subscription/websocket", WebSocketAsync),
            endpoints.MapPost(prefix + "/subscription/$simulate", SimulateAsync),
        };

            if (!string.IsNullOrWhiteSpace(requireScope))
            {
                foreach (var route in routes)
                {
                    route.RequireAuthorization(requireScope);
                }
            }

            return endpoints;
        }
    }

    private static async Task CreateAsync(HttpContext context)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();

        var payload = await JsonSerializer.DeserializeAsync<SubscriptionCreateRequest>(
            context.Request.Body,
            cancellationToken: context.RequestAborted).ConfigureAwait(false);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Topic) || string.IsNullOrWhiteSpace(payload.ChannelEndpoint))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!catalog.TryGet(payload.Topic, out _))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Unknown topic '{payload.Topic}'.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (!Enum.TryParse<SubscriptionChannelType>(payload.ChannelType, ignoreCase: true, out var channelType))
        {
            channelType = SubscriptionChannelType.RestHook;
        }

        var registration = new FhirSubscriptionRegistration(
            Id: Guid.NewGuid().ToString("N"),
            TopicUrl: payload.Topic,
            ChannelType: channelType,
            ChannelEndpoint: payload.ChannelEndpoint,
            ChannelHeader: payload.Secret,
            FilterParameters: payload.Filters ?? new Dictionary<string, string>(StringComparer.Ordinal),
            Status: SubscriptionStatus.Active);

        await registry.RegisterAsync(registration, context.RequestAborted).ConfigureAwait(false);
        context.Response.Headers.Location = $"/fhir/Subscription/{registration.Id}";
        context.Response.StatusCode = StatusCodes.Status201Created;
        await context.Response.WriteAsJsonAsync(registration, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task GetAsync(HttpContext context, string id)
    {
        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();
        var registration = await registry.GetAsync(id, context.RequestAborted).ConfigureAwait(false);
        if (registration is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(registration, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task DeleteAsync(HttpContext context, string id)
    {
        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();
        await registry.DeleteAsync(id, context.RequestAborted).ConfigureAwait(false);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task ListTopicsAsync(HttpContext context)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(catalog.Topics, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task GetTopicAsync(HttpContext context, string name)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        // `name` is the last URL segment used when the topic URL ends with a stable slug.
        var match = catalog.Topics.FirstOrDefault(t => t.Url.EndsWith('/' + name, StringComparison.Ordinal) || t.Url == name);
        if (match is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(match, context.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Showcase/diagnostic trigger: <c>POST {prefix}/subscription/$simulate</c>. Fans a synthetic
    /// (or caller-supplied) FHIR resource through the <em>real</em> matcher + dispatcher pipeline so
    /// the live SSE/WebSocket feed can be demonstrated without driving the upstream clinical
    /// workflow. Inherits the same auth scope as the other Subscription routes.
    /// </summary>
    private static async Task SimulateAsync(HttpContext context)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        var matcher = context.RequestServices.GetRequiredService<ISubscriptionMatcher>();
        var broadcaster = context.RequestServices.GetRequiredService<SubscriptionBroadcaster>();

        SubscriptionSimulateRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<SubscriptionSimulateRequest>(
                context.Request.Body,
                cancellationToken: context.RequestAborted).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            payload = null;
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Topic))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!catalog.TryGet(payload.Topic, out var topic))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Unknown topic '{payload.Topic}'.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var attributes = payload.Attributes is { Count: > 0 }
            ? new Dictionary<string, string>(payload.Attributes, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        Resource resource = BuildSyntheticResource(topic, payload);

        var matched = await matcher.MatchAsync(payload.Topic, attributes, context.RequestAborted).ConfigureAwait(false);
        await broadcaster.BroadcastAsync(payload.Topic, attributes, resource, context.RequestAborted).ConfigureAwait(false);

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsJsonAsync(
            new { topic = payload.Topic, matched = matched.Count, resourceType = resource.TypeName },
            context.RequestAborted).ConfigureAwait(false);
    }

    private static Basic BuildSyntheticResource(SubscriptionTopicDescriptor topic, SubscriptionSimulateRequest payload)
    {
        var label = string.IsNullOrWhiteSpace(payload.Note)
            ? $"{topic.Title} (simulated)"
            : $"{topic.Title} (simulated) — {payload.Note}";
        return new Basic
        {
            Meta = new Meta { Tag = [new Coding("urn:dialysis:fhir:tag", "simulated", "Simulated event")] },
            Code = new CodeableConcept { Text = label },
            Created = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Subject = string.IsNullOrWhiteSpace(payload.Subject) ? null : new ResourceReference(payload.Subject),
        };
    }

    /// <summary>
    /// SSE channel: <c>GET {prefix}/subscription/sse?subscription={id}</c>. Holds the
    /// <c>text/event-stream</c> response open and binds it to the subscription so the SSE channel
    /// dispatcher can push notification Bundles. Blocks until the client disconnects.
    /// </summary>
    private static async Task SseAsync(HttpContext context)
    {
        var subscriptionId = context.Request.Query["subscription"].ToString();
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();
        var registration = await registry.GetAsync(subscriptionId, context.RequestAborted).ConfigureAwait(false);
        if (registration is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var connections = context.RequestServices.GetRequiredService<FhirSubscriptionConnectionManager>();
        var cancellationToken = context.RequestAborted;
        var response = context.Response;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache, no-transform";
        response.Headers.Append("X-Accel-Buffering", "no");
        await response.StartAsync(cancellationToken).ConfigureAwait(false);

        var sink = new ResponseStreamSink(response.Body);
        using (connections.Register(subscriptionId, sink))
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static s => ((TaskCompletionSource)s!).TrySetResult(), tcs))
            {
                try
                {
                    await sink.SendAsync(Encoding.UTF8.GetBytes(": fhir-subscription\n\n"), cancellationToken).ConfigureAwait(false);
                    await connections.FlushReplayAsync(subscriptionId, sink, cancellationToken).ConfigureAwait(false);
                    await tcs.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // client disconnected
                }
            }
        }
    }

    /// <summary>
    /// WebSocket channel: <c>GET {prefix}/subscription/websocket</c>. Implements the Backport IG
    /// text handshake — the client sends <c>bind &lt;subscriptionId&gt;</c>, the server replies
    /// <c>bound &lt;id&gt;</c> and pushes notification Bundles; <c>ping</c>→<c>pong</c> keep-alive;
    /// <c>unbind</c> or socket close ends the binding.
    /// </summary>
    private static async Task WebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();
        var connections = context.RequestServices.GetRequiredService<FhirSubscriptionConnectionManager>();
        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var cancellationToken = context.RequestAborted;
        var sink = new WebSocketSink(socket);
        IDisposable? binding = null;
        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
                if (message.StartsWith("bind ", StringComparison.OrdinalIgnoreCase))
                {
                    var subscriptionId = message[5..].Trim();
                    var registration = await registry.GetAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                    if (registration is null)
                    {
                        await sink.SendTextAsync($"error unknown-subscription {subscriptionId}", cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    binding?.Dispose();
                    binding = connections.Register(subscriptionId, sink);
                    await sink.SendTextAsync($"bound {subscriptionId}", cancellationToken).ConfigureAwait(false);
                    await connections.FlushReplayAsync(subscriptionId, sink, cancellationToken).ConfigureAwait(false);
                }
                else if (message.Equals("ping", StringComparison.OrdinalIgnoreCase))
                {
                    await sink.SendTextAsync("pong", cancellationToken).ConfigureAwait(false);
                }
                else if (message.Equals("unbind", StringComparison.OrdinalIgnoreCase))
                {
                    binding?.Dispose();
                    binding = null;
                    await sink.SendTextAsync("unbound", cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // client or host disconnected
        }
        catch (WebSocketException)
        {
            // abrupt client disconnect
        }
        finally
        {
            binding?.Dispose();
        }
    }

    private sealed record SubscriptionCreateRequest(
        string Topic,
        string ChannelType,
        string ChannelEndpoint,
        string? Secret,
        Dictionary<string, string>? Filters);

    private sealed record SubscriptionSimulateRequest(
        string Topic,
        Dictionary<string, string>? Attributes,
        string? Subject,
        string? Note);

    private sealed class ResponseStreamSink(Stream body) : IFhirSubscriptionSink
    {
        private readonly SemaphoreSlim _write = new(1, 1);

        public async ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
        {
            await _write.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await body.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _write.Release();
            }
        }
    }

    private sealed class WebSocketSink(WebSocket socket) : IFhirSubscriptionSink
    {
        private readonly SemaphoreSlim _write = new(1, 1);

        public async ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
        {
            await _write.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _write.Release();
            }
        }

        public ValueTask SendTextAsync(string text, CancellationToken cancellationToken)
            => SendAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
    }
}
