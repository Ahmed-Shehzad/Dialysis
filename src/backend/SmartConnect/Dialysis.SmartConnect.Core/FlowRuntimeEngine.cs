using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect;

/// <summary>
/// Default <see cref="IFlowRuntime"/> that loads flow definitions, resolves plugins, and writes the <see cref="IMessageLedger"/>.
/// </summary>
public sealed class FlowRuntimeEngine(
    IIntegrationFlowRepository flows,
    IMessageLedger ledger,
    IFlowPluginRegistry plugins,
    TimeProvider time,
    IFlowExecutionContextAccessor? contextAccessor = null,
    ChannelScriptExecutor? scriptExecutor = null,
    AttachmentExtractionPipeline? attachmentExtraction = null,
    AttachmentReattachmentService? attachmentReattachment = null,
    IAlertSink? alertSink = null,
    IServiceScopeFactory? scopeFactory = null) : IFlowRuntime
{
    /// <summary>
    /// Optional metadata key. Source connectors may set this to a JSON object of typed values that the
    /// engine will hydrate into <see cref="FlowExecutionContext.SourceMap"/> (read-only from scripts).
    /// </summary>
    public const string SourceMapMetadataKey = "smartconnect.sourcemap.json";

    /// <summary>
    /// Per-route execution result reduced by the outbound loop. Captures whether the route was
    /// attempted (DSF did not skip it), whether it failed, and the response payload (if any) for
    /// the lowest-ordinal-first selection rule.
    /// </summary>
    private sealed record RouteOutcome(int Ordinal, bool Attempted, bool Failed, string RouteName, byte[]? ResponsePayload);

    public async Task<FlowDispatchResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        await AppendLedgerAsync(
            message,
            MessageLedgerStatus.Received,
            null,
            null,
            message.Payload.ToArray(),
            cancellationToken).ConfigureAwait(false);

        var flow = await flows.GetByIdAsync(message.FlowId, cancellationToken).ConfigureAwait(false);
        if (flow is null)
        {
            return Failure("Integration flow was not found.");
        }

        if (flow.RuntimeState is not FlowRuntimeState.Started)
        {
            return Failure(
                flow.RuntimeState == FlowRuntimeState.Paused
                    ? "Integration flow is paused."
                    : "Integration flow is not started.");
        }

        // Build per-dispatch variable-map context (Mirth Source/Channel/Connector/Response scopes).
        var ctx = BuildFlowExecutionContext(message, flow);
        var previousCtx = contextAccessor?.Current;
        if (contextAccessor is not null)
        {
            contextAccessor.Current = ctx;
        }

        try
        {
            return await DispatchCoreAsync(message, flow, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (contextAccessor is not null)
            {
                contextAccessor.Current = previousCtx;
            }
        }
    }

    private async Task<FlowDispatchResult> DispatchCoreAsync(
        IntegrationMessage message,
        IntegrationFlow flow,
        FlowExecutionContext ctx,
        CancellationToken cancellationToken)
    {

        // PreProcessor script
        var workingMessage = message;
        if (scriptExecutor is not null && !string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.PreProcessorScript))
        {
            var preResult = await scriptExecutor.RunPreProcessorAsync(
                flow.Pipeline.Scripts!.PreProcessorScript!, workingMessage, cancellationToken).ConfigureAwait(false);
            if (preResult.Dropped)
            {
                await AppendLedgerAsync(workingMessage, MessageLedgerStatus.RouteFilterDropped, null, "PreProcessor", null, cancellationToken).ConfigureAwait(false);
                return new FlowDispatchResult { Succeeded = true, Error = null, OutboundRoutesAttempted = [] };
            }

            if (preResult.NewPayload is not null)
            {
                workingMessage = workingMessage.CloneWithPayload(preResult.NewPayload);
            }
        }

        // Attachment Handler — runs once between PreProcessor and route filters. Extracts inline bulky
        // content into the attachment store and rewrites the payload with ${ATTACH:<id>} tokens.
        if (attachmentExtraction is not null && flow.Pipeline.AttachmentHandler is not null)
        {
            var rewritten = await attachmentExtraction
                .RunAsync(workingMessage, flow.Pipeline.AttachmentHandler, cancellationToken).ConfigureAwait(false);
            if (!rewritten.Equals(workingMessage.Payload))
            {
                workingMessage = workingMessage.CloneWithPayload(rewritten);
            }
        }

        ctx.SetCurrentStageContext(CodeTemplateContext.SourceFilter);
        var filterOutcome = await RunRouteFiltersAsync(flow, workingMessage, cancellationToken).ConfigureAwait(false);
        if (filterOutcome is not null)
        {
            return filterOutcome;
        }

        // Source-side transform stages (Mirth source-connector transformer steps; used for Destination Set Filter).
        ctx.SetCurrentStageContext(CodeTemplateContext.SourceTransformer);
        foreach (var stageSlot in flow.Pipeline.SourceTransformStages)
        {
            var stage = plugins.TryResolveTransformStage(stageSlot.Kind);
            if (stage is null)
            {
                return Failure($"Source transform stage kind '{stageSlot.Kind}' is not registered.");
            }

            var workingForStage = string.IsNullOrWhiteSpace(stageSlot.ParametersJson)
                ? workingMessage
                : workingMessage.WithMetadata("smartconnect.transform.parameters", stageSlot.ParametersJson!);
            workingMessage = await stage.TransformAsync(workingForStage, cancellationToken).ConfigureAwait(false);
        }

        var allowedRouteNames = ParseDestinationSet(workingMessage);
        var routes = flow.Pipeline.OutboundRoutes;
        var sequential = flow.Pipeline.OutboundRoutesSequential;
        RouteOutcome[] outcomes;

        if (sequential)
        {
            // Mirth-style destination chain: routes run in list order; first failure stops later routes.
            var list = new List<RouteOutcome>(routes.Count);
            for (var i = 0; i < routes.Count; i++)
            {
                ctx.SetCurrentRouteOrdinal(i);
                var outcome = await ExecuteRouteAsync(workingMessage, routes[i], i, allowedRouteNames, ctx, useScopedLedger: false, cancellationToken).ConfigureAwait(false);
                list.Add(outcome);
                if (outcome.Failed)
                {
                    break;
                }
            }
            outcomes = list.ToArray();
        }
        else
        {
            // Mirth-style parallel destinations: every route runs concurrently. Per-route side-effects
            // (ledger writes, alert publishes) run inside a fresh DI scope when scopeFactory is available
            // so the engine's scoped DbContext is not shared across worker tasks — same pattern as
            // PublishAlert, fixed in PR #92 after ChangeTracker races surfaced under fire-and-forget.
            //
            // CurrentRouteOrdinal/CurrentStageContext are scalar AsyncLocals on FlowExecutionContext;
            // they race meaningfully across parallel tasks. Scripts running inside per-route transform
            // stages must read ordinal/stage from arguments handed in by the runtime, not from the
            // accessor. Sequential mode keeps the per-route accessor semantics unchanged.
            var tasks = new Task<RouteOutcome>[routes.Count];
            for (var i = 0; i < routes.Count; i++)
            {
                var ordinal = i;
                var route = routes[i];
                tasks[i] = Task.Run(
                    () => ExecuteRouteAsync(workingMessage, route, ordinal, allowedRouteNames, ctx, useScopedLedger: true, cancellationToken),
                    cancellationToken);
            }
            outcomes = await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        ctx.SetCurrentRouteOrdinal(-1);

        var attempted = new List<int>(outcomes.Length);
        var anyOutboundFailed = false;
        byte[]? responsePayload = null;
        foreach (var outcome in outcomes.OrderBy(o => o.Ordinal))
        {
            if (outcome.Attempted)
            {
                attempted.Add(outcome.Ordinal);
            }
            if (outcome.Failed)
            {
                anyOutboundFailed = true;
            }
            // First-wins by route ordinal (parallel has no first-in-time; ordinal preserves a stable rule).
            responsePayload ??= outcome.ResponsePayload;
        }

        await AppendLedgerAsync(
            workingMessage,
            MessageLedgerStatus.Completed,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        // PostProcessor script
        var overallSuccess = !anyOutboundFailed;
        if (scriptExecutor is not null && !string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.PostProcessorScript))
        {
            await scriptExecutor.RunPostProcessorAsync(
                flow.Pipeline.Scripts!.PostProcessorScript!, workingMessage, overallSuccess, cancellationToken).ConfigureAwait(false);
        }

        return new FlowDispatchResult
        {
            Succeeded = overallSuccess,
            Error = anyOutboundFailed ? "One or more outbound routes failed." : null,
            OutboundRoutesAttempted = attempted,
            ResponsePayload = responsePayload,
        };
    }

    private async Task<FlowDispatchResult?> RunRouteFiltersAsync(
        IntegrationFlow flow,
        IntegrationMessage message,
        CancellationToken cancellationToken)
    {
        foreach (var slot in flow.Pipeline.RouteFilters)
        {
            var filter = plugins.TryResolveRouteFilter(slot.Kind);
            if (filter is null)
            {
                return Failure($"Route filter kind '{slot.Kind}' is not registered.");
            }

            var messageForFilter = string.IsNullOrWhiteSpace(slot.ParametersJson)
                ? message
                : message.WithMetadata("smartconnect.filter.parameters", slot.ParametersJson!);

            var result = await filter.EvaluateAsync(messageForFilter, cancellationToken).ConfigureAwait(false);
            if (result.Disposition != RouteFilterDisposition.Drop)
            {
                continue;
            }

            await AppendLedgerAsync(
                message,
                MessageLedgerStatus.RouteFilterDropped,
                null,
                slot.Kind,
                null,
                cancellationToken).ConfigureAwait(false);

            return new FlowDispatchResult { Succeeded = true, Error = null, OutboundRoutesAttempted = [] };
        }

        return null;
    }

    private async Task<(IntegrationMessage? Message, string? ErrorDetail)> TryTransformForRouteAsync(
        IntegrationMessage message,
        OutboundRouteSlot route,
        CancellationToken cancellationToken)
    {
        var working = message;
        foreach (var stageSlot in route.TransformStages)
        {
            var stage = plugins.TryResolveTransformStage(stageSlot.Kind);
            if (stage is null)
            {
                return (null, $"Transform stage kind '{stageSlot.Kind}' is not registered.");
            }

            var workingForStage = string.IsNullOrWhiteSpace(stageSlot.ParametersJson)
                ? working
                : working.WithMetadata("smartconnect.transform.parameters", stageSlot.ParametersJson!);
            working = await stage.TransformAsync(workingForStage, cancellationToken).ConfigureAwait(false);
        }

        return (working, null);
    }

    /// <summary>
    /// Runs one outbound route: DSF skip check, adapter resolve, per-route transforms, attachment
    /// re-attach, retry-with-backoff send, response transform, per-route ledger writes. Returns a
    /// <see cref="RouteOutcome"/> the dispatch loop reduces into the final <see cref="FlowDispatchResult"/>.
    ///
    /// When <paramref name="useScopedLedger"/> is true (parallel mode), per-route ledger writes go
    /// through a fresh <see cref="IServiceScope"/> so the engine's scoped DbContext is not shared
    /// across worker tasks. Same pattern as <see cref="PublishAlert"/> — see PR #92.
    /// </summary>
    private async Task<RouteOutcome> ExecuteRouteAsync(
        IntegrationMessage workingMessage,
        OutboundRouteSlot route,
        int ordinal,
        HashSet<string>? allowedRouteNames,
        FlowExecutionContext ctx,
        bool useScopedLedger,
        CancellationToken cancellationToken)
    {
        var resolvedRouteName = ResolveRouteName(route, ordinal);

        // Destination Set Filter — skip routes the source transform excluded.
        if (allowedRouteNames is not null && !allowedRouteNames.Contains(resolvedRouteName))
        {
            await WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.RouteFilterDropped,
                $"Skipped by destination set filter (route '{resolvedRouteName}' not in allowed set).",
                null,
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            return new RouteOutcome(ordinal, Attempted: false, Failed: false, resolvedRouteName, null);
        }

        var outbound = plugins.TryResolveOutboundAdapter(route.OutboundAdapterKind);
        if (outbound is null)
        {
            var detail = $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.";
            await WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.OutboundFailed,
                detail,
                null,
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            PublishAlert(workingMessage, AlertErrorType.OutboundFailure, detail, cancellationToken);
            return new RouteOutcome(ordinal, Attempted: true, Failed: true, resolvedRouteName, null);
        }

        ctx.SetCurrentStageContext(CodeTemplateContext.DestinationTransformer);
        var transformed = await TryTransformForRouteAsync(workingMessage, route, cancellationToken).ConfigureAwait(false);
        if (transformed.ErrorDetail is not null)
        {
            await WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.OutboundFailed,
                transformed.ErrorDetail,
                null,
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            PublishAlert(workingMessage, AlertErrorType.TransformError, transformed.ErrorDetail, cancellationToken);
            return new RouteOutcome(ordinal, Attempted: true, Failed: true, resolvedRouteName, null);
        }

        var toSend = transformed.Message!;
        if (!string.IsNullOrWhiteSpace(route.OutboundParametersJson))
        {
            toSend = toSend.WithMetadata("smartconnect.outbound.parameters", route.OutboundParametersJson!);
        }

        // Reattach Attachments — inflate ${ATTACH:<id>} tokens back to raw bytes if the route opted in.
        if (route.ReattachAttachments && attachmentReattachment is not null)
        {
            var inflated = await attachmentReattachment
                .InflateAsync(toSend.Payload, workingMessage.Id, cancellationToken).ConfigureAwait(false);
            if (!inflated.Equals(toSend.Payload))
            {
                toSend = toSend.CloneWithPayload(inflated);
            }
        }

        var maxAttempts = route.MaxAttempts < 1 ? 1 : route.MaxAttempts;
        var sendSucceeded = false;
        string? sendError = null;
        OutboundSendResult lastSendResult = default;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastSendResult = await outbound.SendAsync(toSend, ordinal, cancellationToken).ConfigureAwait(false);
            if (lastSendResult.Succeeded)
            {
                sendSucceeded = true;
                break;
            }

            sendError = lastSendResult.ErrorDetail ?? "Outbound send failed.";
            if (attempt < maxAttempts)
            {
                var delayMs = 100 * Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken).ConfigureAwait(false);
            }
        }

        if (!sendSucceeded)
        {
            ctx.ResponseMap[resolvedRouteName] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["status"] = "failure",
                ["error"] = sendError,
            };
            await WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.OutboundFailed,
                sendError,
                toSend.Payload.ToArray(),
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            PublishAlert(workingMessage, AlertErrorType.OutboundFailure, sendError, cancellationToken);
            return new RouteOutcome(ordinal, Attempted: true, Failed: true, resolvedRouteName, null);
        }

        ctx.ResponseMap[resolvedRouteName] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = "success",
            ["payload"] = lastSendResult.ResponsePayload is { Length: > 0 } bytes
                ? Encoding.UTF8.GetString(bytes)
                : null,
        };
        await WriteOutboundLedgerScopedAsync(
            workingMessage,
            ordinal,
            MessageLedgerStatus.OutboundSent,
            null,
            toSend.Payload.ToArray(),
            useScopedLedger,
            cancellationToken).ConfigureAwait(false);

        byte[]? routeResponsePayload = null;
        if (lastSendResult.ResponsePayload is { Length: > 0 } rawResp && route.ResponseTransformStages.Count > 0)
        {
            ctx.SetCurrentStageContext(CodeTemplateContext.DestinationResponseTransformer);
            var respMsg = workingMessage.CloneWithPayload(rawResp);
            foreach (var stage in route.ResponseTransformStages)
            {
                var transformer = plugins.TryResolveTransformStage(stage.Kind);
                if (transformer is null) continue;
                var tMsg = !string.IsNullOrWhiteSpace(stage.ParametersJson)
                    ? respMsg.WithMetadata("smartconnect.transform.parameters", stage.ParametersJson!)
                    : respMsg;
                respMsg = await transformer.TransformAsync(tMsg, cancellationToken).ConfigureAwait(false);
            }
            routeResponsePayload = respMsg.Payload.ToArray();
        }
        else if (lastSendResult.ResponsePayload is { Length: > 0 } directResp)
        {
            routeResponsePayload = directResp;
        }

        return new RouteOutcome(ordinal, Attempted: true, Failed: false, resolvedRouteName, routeResponsePayload);
    }

    private Task AppendLedgerAsync(
        IntegrationMessage message,
        MessageLedgerStatus status,
        int? outboundRouteOrdinal,
        string? detail,
        byte[]? snapshot,
        CancellationToken cancellationToken) =>
        ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                FlowId = message.FlowId,
                IntegrationMessageId = message.Id,
                CorrelationId = message.CorrelationId,
                Status = status,
                OutboundRouteOrdinal = outboundRouteOrdinal,
                Detail = detail,
                PayloadSnapshot = snapshot,
                Metadata = message.Metadata,
                CreatedAtUtc = time.GetUtcNow(),
            },
            cancellationToken);

    /// <summary>
    /// Like <see cref="WriteOutboundLedgerAsync"/> but opens a fresh DI scope and resolves the
    /// ledger from it when <paramref name="useScopedLedger"/> is true and a <c>scopeFactory</c>
    /// is wired. Used by parallel outbound dispatch so the engine's scoped DbContext is never
    /// shared across worker tasks (the ChangeTracker race PR #92 fixed for alerts).
    /// </summary>
    private async Task WriteOutboundLedgerScopedAsync(
        IntegrationMessage message,
        int routeOrdinal,
        MessageLedgerStatus status,
        string? detail,
        byte[]? snapshot,
        bool useScopedLedger,
        CancellationToken cancellationToken)
    {
        if (useScopedLedger && scopeFactory is not null)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var scopedLedger = scope.ServiceProvider.GetService<IMessageLedger>() ?? ledger;
            await scopedLedger.AppendAsync(
                new MessageLedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    FlowId = message.FlowId,
                    IntegrationMessageId = message.Id,
                    CorrelationId = message.CorrelationId,
                    Status = status,
                    OutboundRouteOrdinal = routeOrdinal,
                    Detail = detail,
                    PayloadSnapshot = snapshot,
                    Metadata = message.Metadata,
                    CreatedAtUtc = time.GetUtcNow(),
                },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await AppendLedgerAsync(message, status, routeOrdinal, detail, snapshot, cancellationToken).ConfigureAwait(false);
    }

    private static FlowDispatchResult Failure(string error) =>
        new() { Succeeded = false, Error = error, OutboundRoutesAttempted = [] };

    private void PublishAlert(IntegrationMessage message, AlertErrorType errorType, string? errorDetail, CancellationToken cancellationToken)
    {
        if (alertSink is null) return;
        var trigger = new AlertTrigger
        {
            FlowId = message.FlowId,
            MessageId = message.Id,
            CorrelationId = message.CorrelationId,
            ErrorType = errorType,
            ErrorDetail = errorDetail,
            OccurredAtUtc = time.GetUtcNow(),
        };
        // Fire-and-forget: alerts must never block the dispatch path. When a scope factory is
        // available, the background task runs the alert sink in a fresh DI scope so its EF
        // bookkeeping (alert-event store, rule repository) gets its own DbContext — sharing the
        // dispatcher's scoped DbContext would race with the in-flight ledger save and surface as
        // "Collection was modified during enumeration" inside ChangeTracker.DetectChanges.
        //
        // Fallback to the captured singleton-style alertSink when no scope factory is wired (legacy
        // tests that compose the runtime by hand without a DI container). The legacy path remains
        // safe only when the consumer doesn't share a DbContext between threads.
        _ = Task.Run(async () =>
        {
            try
            {
                if (scopeFactory is not null)
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var scopedSink = scope.ServiceProvider.GetService<IAlertSink>() ?? alertSink;
                    await scopedSink.PublishAsync(trigger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await alertSink.PublishAsync(trigger, cancellationToken).ConfigureAwait(false);
                }
            }
            catch { /* swallowed: alert engine logs internally */ }
        }, cancellationToken);
    }

    private static HashSet<string>? ParseDestinationSet(IntegrationMessage message)
    {
        if (!message.Metadata.TryGetValue(DestinationSetFilterTransformStage.DestinationSetMetadataKey, out var csv))
            return null;
        if (string.IsNullOrEmpty(csv))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            result.Add(name);
        return result;
    }

    private static string ResolveRouteName(OutboundRouteSlot route, int index)
    {
        if (!string.IsNullOrWhiteSpace(route.OutboundParametersJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(route.OutboundParametersJson!);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("routeName", out var nameEl) &&
                    nameEl.ValueKind == JsonValueKind.String)
                {
                    var named = nameEl.GetString();
                    if (!string.IsNullOrWhiteSpace(named))
                        return named;
                }
            }
            catch (JsonException)
            {
                // Fall through to the index-based fallback.
            }
        }

        return $"route-{index}";
    }

    private static FlowExecutionContext BuildFlowExecutionContext(IntegrationMessage message, IntegrationFlow flow)
    {
        var sourceMap = ParseSourceMap(message);
        var connectorMaps = new ConcurrentDictionary<string, object?>[flow.Pipeline.OutboundRoutes.Count];
        for (var i = 0; i < connectorMaps.Length; i++)
        {
            connectorMaps[i] = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        }

        return new FlowExecutionContext
        {
            MessageId = message.Id,
            FlowId = message.FlowId,
            SourceMap = sourceMap,
            ConnectorMaps = connectorMaps,
        };
    }

    private static IReadOnlyDictionary<string, object?> ParseSourceMap(IntegrationMessage message)
    {
        if (!message.Metadata.TryGetValue(SourceMapMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return ImmutableDictionary<string, object?>.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ImmutableDictionary<string, object?>.Empty;
            }

            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = JsonElementToObject(prop.Value);
            }

            return dict;
        }
        catch (JsonException)
        {
            return ImmutableDictionary<string, object?>.Empty;
        }
    }

    private static object? JsonElementToObject(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                return el.TryGetInt64(out var l) ? (object)l : el.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Object:
            {
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var p in el.EnumerateObject())
                {
                    dict[p.Name] = JsonElementToObject(p.Value);
                }
                return dict;
            }
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in el.EnumerateArray())
                {
                    list.Add(JsonElementToObject(item));
                }
                return list;
            }
            default:
                return null;
        }
    }
}
