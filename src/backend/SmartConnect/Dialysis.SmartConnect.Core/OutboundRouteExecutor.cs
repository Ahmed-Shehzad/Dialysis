using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Endpoints;
using Dialysis.SmartConnect.VariableMaps;

namespace Dialysis.SmartConnect;

/// <summary>
/// Executes a single outbound route for the flow runtime: DSF skip check, adapter resolve,
/// per-route transforms, attachment re-attach, retry-with-backoff send, response transform,
/// and per-route ledger writes.
/// </summary>
internal sealed class OutboundRouteExecutor
{
    private readonly IFlowPluginRegistry _plugins;
    private readonly FlowLedgerWriter _ledgerWriter;
    private readonly FlowAlertPublisher _alertPublisher;
    private readonly AttachmentReattachmentService? _attachmentReattachment;
    private readonly IEndpointResolver? _endpointResolver;

    /// <summary>
    /// Executes a single outbound route for the flow runtime: DSF skip check, adapter resolve,
    /// per-route transforms, attachment re-attach, retry-with-backoff send, response transform,
    /// and per-route ledger writes.
    /// </summary>
    public OutboundRouteExecutor(
        IFlowPluginRegistry plugins,
        FlowLedgerWriter ledgerWriter,
        FlowAlertPublisher alertPublisher,
        AttachmentReattachmentService? attachmentReattachment,
        IEndpointResolver? endpointResolver)
    {
        _plugins = plugins;
        _ledgerWriter = ledgerWriter;
        _alertPublisher = alertPublisher;
        _attachmentReattachment = attachmentReattachment;
        _endpointResolver = endpointResolver;
    }

    /// <summary>
    /// Runs one outbound route: DSF skip check, adapter resolve, per-route transforms, attachment
    /// re-attach, retry-with-backoff send, response transform, per-route ledger writes. Returns a
    /// <see cref="RouteOutcome"/> the dispatch loop reduces into the final <see cref="FlowDispatchResult"/>.
    ///
    /// When <paramref name="useScopedLedger"/> is true (parallel mode), per-route ledger writes go
    /// through a fresh <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/> so the
    /// engine's scoped DbContext is not shared across worker tasks. Same pattern as
    /// <see cref="FlowAlertPublisher.Publish"/> — see PR #92.
    /// </summary>
    public async Task<RouteOutcome> ExecuteRouteAsync(
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
            await _ledgerWriter.WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.RouteFilterDropped,
                $"Skipped by destination set filter (route '{resolvedRouteName}' not in allowed set).",
                null,
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            return new RouteOutcome(ordinal, Attempted: false, Failed: false, resolvedRouteName, null);
        }

        var outbound = _plugins.TryResolveOutboundAdapter(route.OutboundAdapterKind);
        if (outbound is null)
        {
            var detail = $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.";
            await _ledgerWriter.WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.OutboundFailed,
                detail,
                null,
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            _alertPublisher.Publish(workingMessage, AlertErrorType.OutboundFailure, detail, cancellationToken);
            return new RouteOutcome(ordinal, Attempted: true, Failed: true, resolvedRouteName, null);
        }

        ctx.SetCurrentStageContext(CodeTemplateContext.DestinationTransformer);
        var transformed = await TryTransformForRouteAsync(workingMessage, route, cancellationToken).ConfigureAwait(false);
        if (transformed.ErrorDetail is not null)
        {
            await _ledgerWriter.WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.OutboundFailed,
                transformed.ErrorDetail,
                null,
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            _alertPublisher.Publish(workingMessage, AlertErrorType.TransformError, transformed.ErrorDetail, cancellationToken);
            return new RouteOutcome(ordinal, Attempted: true, Failed: true, resolvedRouteName, null);
        }

        var toSend = transformed.Message!;
        var resolvedParametersJson = route.OutboundParametersJson;
        if (_endpointResolver is not null && !string.IsNullOrWhiteSpace(resolvedParametersJson))
        {
            resolvedParametersJson = await _endpointResolver
                .ResolveParametersJsonAsync(resolvedParametersJson, cancellationToken)
                .ConfigureAwait(false);
        }
        if (!string.IsNullOrWhiteSpace(resolvedParametersJson))
        {
            toSend = toSend.WithMetadata("smartconnect.outbound.parameters", resolvedParametersJson!);
        }

        // Reattach Attachments — inflate ${ATTACH:<id>} tokens back to raw bytes if the route opted in.
        if (route.ReattachAttachments && _attachmentReattachment is not null)
        {
            var inflated = await _attachmentReattachment
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
            await _ledgerWriter.WriteOutboundLedgerScopedAsync(
                workingMessage,
                ordinal,
                MessageLedgerStatus.OutboundFailed,
                sendError,
                toSend.Payload.ToArray(),
                useScopedLedger,
                cancellationToken).ConfigureAwait(false);
            _alertPublisher.Publish(workingMessage, AlertErrorType.OutboundFailure, sendError, cancellationToken);
            return new RouteOutcome(ordinal, Attempted: true, Failed: true, resolvedRouteName, null);
        }

        ctx.ResponseMap[resolvedRouteName] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = "success",
            ["payload"] = lastSendResult.ResponsePayload is { Length: > 0 } bytes
                ? Encoding.UTF8.GetString(bytes)
                : null,
        };
        await _ledgerWriter.WriteOutboundLedgerScopedAsync(
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
                var transformer = _plugins.TryResolveTransformStage(stage.Kind);
                if (transformer is null)
                    continue;
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

    private async Task<(IntegrationMessage? Message, string? ErrorDetail)> TryTransformForRouteAsync(
        IntegrationMessage message,
        OutboundRouteSlot route,
        CancellationToken cancellationToken)
    {
        var working = message;
        foreach (var stageSlot in route.TransformStages)
        {
            var stage = _plugins.TryResolveTransformStage(stageSlot.Kind);
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
