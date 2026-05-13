using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Persistence;
using Dialysis.SmartConnect.Scripts;

namespace Dialysis.SmartConnect;

/// <summary>
/// Default <see cref="IFlowRuntime"/> that loads flow definitions, resolves plugins, and writes the <see cref="IMessageLedger"/>.
/// </summary>
public sealed class FlowRuntimeEngine(
    IIntegrationFlowRepository flows,
    IMessageLedger ledger,
    IFlowPluginRegistry plugins,
    TimeProvider time,
    ChannelScriptExecutor? scriptExecutor = null) : IFlowRuntime
{
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

        var filterOutcome = await RunRouteFiltersAsync(flow, workingMessage, cancellationToken).ConfigureAwait(false);
        if (filterOutcome is not null)
        {
            return filterOutcome;
        }

        // Source-side transform stages (Mirth source-connector transformer steps; used for Destination Set Filter).
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
            var route = flow.Pipeline.OutboundRoutes[i];

            // Destination Set Filter — skip routes the source transform excluded.
            if (allowedRouteNames is not null)
            {
                var routeName = ResolveRouteName(route, i);
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
                await WriteOutboundLedgerAsync(
                    workingMessage,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.",
                    null,
                    cancellationToken).ConfigureAwait(false);
                if (sequential)
                {
                    break;
                }

                continue;
            }

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
                await WriteOutboundLedgerAsync(
                    workingMessage,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    sendError,
                    toSend.Payload.ToArray(),
                    cancellationToken).ConfigureAwait(false);
                if (sequential)
                {
                    break;
                }
            }
            else
            {
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
}
