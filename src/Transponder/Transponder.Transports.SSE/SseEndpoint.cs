using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

internal static class SseEndpoint
{
    public async static Task HandleAsync(
        HttpContext context,
        ISseHostSettings settings,
        SseClientRegistry registry,
        ISseCatchUpProvider? catchUpProvider,
        SseEndpointOptions options)
    {
        if (options.RequireEventStreamAcceptHeader &&
            !AcceptsEventStream(context.Request.Headers.Accept.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
            return;
        }

        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Response.ContentType = "text/event-stream";

        var connectionId = Ulid.NewUlid().ToString();
        var userId = ResolveUserId(context, options);
        var streams = GetQueryValues(context, options.StreamQueryKey);
        var groups = GetQueryValues(context, options.GroupQueryKey);

        if (streams.Count == 0) streams = ["all"];

        var connection = new SseClientConnection(
            connectionId,
            userId,
            streams,
            groups,
            settings.ClientBufferCapacity);

        registry.Register(connection);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted);

        Task? keepAliveTask = null;
        if (settings.KeepAliveInterval.HasValue && settings.KeepAliveInterval.Value > TimeSpan.Zero)
            keepAliveTask = StartKeepAliveAsync(connection, settings.KeepAliveInterval.Value, linkedCts.Token);

        try
        {
            if (options.SendConnectionEventOnConnect)
            {
                var payload = $"{{\"connectionId\":\"{connectionId}\"}}";
                _ = connection.TryEnqueue(new SseEvent(
                    id: null,
                    eventName: options.ConnectionEventName,
                    data: payload,
                    comment: null));
            }

            if (catchUpProvider is not null)
            {
                var lastEventId = GetLastEventId(context);
                var request = new SseCatchUpRequest(lastEventId, userId, streams, groups);
                var replayEvents = await catchUpProvider
                    .GetEventsAsync(request, context.RequestAborted)
                    .ConfigureAwait(false);

                foreach (var replay in replayEvents)
                    _ = connection.TryEnqueue(new SseEvent(
                        replay.Id,
                        replay.EventName,
                        replay.Data,
                        comment: null));
            }

            await foreach (var message in connection.Reader.ReadAllAsync(context.RequestAborted))
                await SseEventWriter.WriteAsync(context.Response.Body, message, context.RequestAborted)
                    .ConfigureAwait(false);
        }
        finally
        {
            await linkedCts.CancelAsync();
            if (keepAliveTask is not null)
                try { await keepAliveTask.ConfigureAwait(false); } catch (OperationCanceledException) { }

            connection.Complete();
            registry.Unregister(connection);
        }
    }

    private static bool AcceptsEventStream(string? acceptHeader)
        => !string.IsNullOrWhiteSpace(acceptHeader) &&
           acceptHeader.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase);

    private static string? ResolveUserId(HttpContext context, SseEndpointOptions options)
    {
        if (context.Request.Query.TryGetValue(options.UserQueryKey, out var values))
        {
            var explicitUser = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(explicitUser)) return explicitUser;
        }

        return context.User?.Identity?.Name;
    }

    private static IReadOnlyList<string> GetQueryValues(HttpContext context, string key)
    {
        if (!context.Request.Query.TryGetValue(key, out var values) || values.Count == 0)
            return [];

        var results = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var segments = value.Split(
                [',', ';'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            results.AddRange(segments);
        }

        return results.Count == 0 ? results : results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? GetLastEventId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Last-Event-ID", out var values))
        {
            var headerValue = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue)) return headerValue;
        }

        if (context.Request.Query.TryGetValue("lastEventId", out var queryValues))
        {
            var queryValue = queryValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(queryValue)) return queryValue;
        }

        return null;
    }

    private static Task StartKeepAliveAsync(
        SseClientConnection connection,
        TimeSpan interval,
        CancellationToken cancellationToken)
        => Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                _ = connection.TryEnqueue(SseEvent.CreateComment("keep-alive"));
        }, cancellationToken);
}
