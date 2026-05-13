using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Persistence;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;

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
    IAlertSink? alertSink = null) : IFlowRuntime
{
    /// <summary>
    /// Optional metadata key. Source connectors may set this to a JSON object of typed values that the
    /// engine will hydrate into <see cref="FlowExecutionContext.SourceMap"/> (read-only from scripts).
    /// </summary>
    public const string SourceMapMetadataKey = "smartconnect.sourcemap.json";

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
        var attempted = new List<int>();
        var anyOutboundFailed = false;
        byte[]? responsePayload = null;
        var sequential = flow.Pipeline.OutboundRoutesSequential;
        for (var i = 0; i < flow.Pipeline.OutboundRoutes.Count; i++)
        {
            ctx.SetCurrentRouteOrdinal(i);
            var route = flow.Pipeline.OutboundRoutes[i];
            var resolvedRouteName = ResolveRouteName(route, i);

            // Destination Set Filter — skip routes the source transform excluded.
            if (allowedRouteNames is not null)
            {
                var routeName = resolvedRouteName;
                if (!allowedRouteNames.Contains(routeName))
                {
                    await WriteOutboundLedgerAsync(
                        workingMessage,
                        i,
                        MessageLedgerStatus.RouteFilterDropped,
                        $"Skipped by destination set filter (route '{routeName}' not in allowed set).",
                        null,
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }
            }

            attempted.Add(i);
            var outbound = plugins.TryResolveOutboundAdapter(route.OutboundAdapterKind);
            if (outbound is null)
            {
                anyOutboundFailed = true;
                var detail = $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.";
                await WriteOutboundLedgerAsync(
                    workingMessage,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    detail,
                    null,
                    cancellationToken).ConfigureAwait(false);
                PublishAlert(workingMessage, AlertErrorType.OutboundFailure, detail, cancellationToken);
                if (sequential)
                {
                    break;
                }

                continue;
            }

            ctx.SetCurrentStageContext(CodeTemplateContext.DestinationTransformer);
            var transformed = await TryTransformForRouteAsync(workingMessage, route, cancellationToken).ConfigureAwait(false);
            if (transformed.ErrorDetail is not null)
            {
                anyOutboundFailed = true;
                await WriteOutboundLedgerAsync(
                    workingMessage,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    transformed.ErrorDetail,
                    null,
                    cancellationToken).ConfigureAwait(false);
                PublishAlert(workingMessage, AlertErrorType.TransformError, transformed.ErrorDetail, cancellationToken);
                if (sequential)
                {
                    break;
                }

                continue;
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
                lastSendResult = await outbound.SendAsync(toSend, i, cancellationToken).ConfigureAwait(false);
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
                anyOutboundFailed = true;
                ctx.ResponseMap[resolvedRouteName] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["status"] = "failure",
                    ["error"] = sendError,
                };
                await WriteOutboundLedgerAsync(
                    workingMessage,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    sendError,
                    toSend.Payload.ToArray(),
                    cancellationToken).ConfigureAwait(false);
                PublishAlert(workingMessage, AlertErrorType.OutboundFailure, sendError, cancellationToken);
                if (sequential)
                {
                    break;
                }
            }
            else
            {
                ctx.ResponseMap[resolvedRouteName] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["status"] = "success",
                    ["payload"] = lastSendResult.ResponsePayload is { Length: > 0 } bytes
                        ? Encoding.UTF8.GetString(bytes)
                        : null,
                };
                await WriteOutboundLedgerAsync(
                    workingMessage,
                    i,
                    MessageLedgerStatus.OutboundSent,
                    null,
                    toSend.Payload.ToArray(),
                    cancellationToken).ConfigureAwait(false);

                // Response transform: apply ResponseTransformStages if present
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

                    responsePayload ??= respMsg.Payload.ToArray();
                }
                else if (lastSendResult.ResponsePayload is { Length: > 0 } directResp)
                {
                    responsePayload ??= directResp;
                }
            }
        }

        ctx.SetCurrentRouteOrdinal(-1);

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
                CreatedAtUtc = time.GetUtcNow(),
            },
            cancellationToken);

    private Task WriteOutboundLedgerAsync(
        IntegrationMessage message,
        int routeOrdinal,
        MessageLedgerStatus status,
        string? detail,
        byte[]? snapshot,
        CancellationToken cancellationToken) =>
        AppendLedgerAsync(message, status, routeOrdinal, detail, snapshot, cancellationToken);

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
        // Fire-and-forget: alerts must never block the dispatch path.
        _ = Task.Run(async () =>
        {
            try { await alertSink.PublishAsync(trigger, cancellationToken).ConfigureAwait(false); }
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
