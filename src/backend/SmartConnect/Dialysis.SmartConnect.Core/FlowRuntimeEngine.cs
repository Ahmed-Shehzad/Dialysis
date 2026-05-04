using Dialysis.SmartConnect.Persistence;

namespace Dialysis.SmartConnect;

/// <summary>
/// Default <see cref="IFlowRuntime"/> that loads flow definitions, resolves plugins, and writes the <see cref="IMessageLedger"/>.
/// </summary>
public sealed class FlowRuntimeEngine(
    IIntegrationFlowRepository flows,
    IMessageLedger ledger,
    IFlowPluginRegistry plugins,
    TimeProvider time) : IFlowRuntime
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

        var filterOutcome = await RunRouteFiltersAsync(flow, message, cancellationToken).ConfigureAwait(false);
        if (filterOutcome is not null)
        {
            return filterOutcome;
        }

        var attempted = new List<int>();
        var anyOutboundFailed = false;
        for (var i = 0; i < flow.Pipeline.OutboundRoutes.Count; i++)
        {
            var route = flow.Pipeline.OutboundRoutes[i];
            attempted.Add(i);
            var outbound = plugins.TryResolveOutboundAdapter(route.OutboundAdapterKind);
            if (outbound is null)
            {
                anyOutboundFailed = true;
                await WriteOutboundLedgerAsync(
                    message,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.",
                    null,
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            var transformed = await TryTransformForRouteAsync(message, route, cancellationToken).ConfigureAwait(false);
            if (transformed.ErrorDetail is not null)
            {
                anyOutboundFailed = true;
                await WriteOutboundLedgerAsync(
                    message,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    transformed.ErrorDetail,
                    null,
                    cancellationToken).ConfigureAwait(false);
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
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var send = await outbound.SendAsync(toSend, i, cancellationToken).ConfigureAwait(false);
                if (send.Succeeded)
                {
                    sendSucceeded = true;
                    break;
                }

                sendError = send.ErrorDetail ?? "Outbound send failed.";
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
                    message,
                    i,
                    MessageLedgerStatus.OutboundFailed,
                    sendError,
                    toSend.Payload.ToArray(),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteOutboundLedgerAsync(
                    message,
                    i,
                    MessageLedgerStatus.OutboundSent,
                    null,
                    toSend.Payload.ToArray(),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await AppendLedgerAsync(
            message,
            MessageLedgerStatus.Completed,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        return new FlowDispatchResult
        {
            Succeeded = !anyOutboundFailed,
            Error = anyOutboundFailed ? "One or more outbound routes failed." : null,
            OutboundRoutesAttempted = attempted,
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

            var result = await filter.EvaluateAsync(message, cancellationToken).ConfigureAwait(false);
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
}
