using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Endpoints;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect;

/// <summary>
/// Default <see cref="IFlowRuntime"/> that loads flow definitions, resolves plugins, and writes the <see cref="IMessageLedger"/>.
/// Orchestration façade: source-side stages run through <see cref="SourceStageProcessor"/>, per-route outbound
/// execution through <see cref="OutboundRouteExecutor"/>, ledger writes through <see cref="FlowLedgerWriter"/>,
/// and alert publication through <see cref="FlowAlertPublisher"/>.
/// </summary>
public sealed class FlowRuntimeEngine : IFlowRuntime
{
    private readonly IIntegrationFlowRepository _flows;
    private readonly IFlowExecutionContextAccessor? _contextAccessor;
    private readonly ChannelScriptExecutor? _scriptExecutor;
    private readonly FlowLedgerWriter _ledgerWriter;
    private readonly SourceStageProcessor _sourceStages;
    private readonly OutboundRouteExecutor _routeExecutor;

    /// <summary>
    /// Default <see cref="IFlowRuntime"/> that loads flow definitions, resolves plugins, and writes the <see cref="IMessageLedger"/>.
    /// </summary>
    public FlowRuntimeEngine(IIntegrationFlowRepository flows,
        IMessageLedger ledger,
        IFlowPluginRegistry plugins,
        TimeProvider time,
        IFlowExecutionContextAccessor? contextAccessor = null,
        ChannelScriptExecutor? scriptExecutor = null,
        AttachmentExtractionPipeline? attachmentExtraction = null,
        AttachmentReattachmentService? attachmentReattachment = null,
        IAlertSink? alertSink = null,
        IServiceScopeFactory? scopeFactory = null,
        IEndpointResolver? endpointResolver = null)
    {
        _flows = flows;
        _contextAccessor = contextAccessor;
        _scriptExecutor = scriptExecutor;
        _ledgerWriter = new FlowLedgerWriter(ledger, time, scopeFactory);
        var alertPublisher = new FlowAlertPublisher(alertSink, time, scopeFactory);
        _sourceStages = new SourceStageProcessor(plugins, _ledgerWriter, scriptExecutor, attachmentExtraction);
        _routeExecutor = new OutboundRouteExecutor(plugins, _ledgerWriter, alertPublisher, attachmentReattachment, endpointResolver);
    }

    /// <summary>
    /// Optional metadata key. Source connectors may set this to a JSON object of typed values that the
    /// engine will hydrate into <see cref="FlowExecutionContext.SourceMap"/> (read-only from scripts).
    /// </summary>
    public const string SourceMapMetadataKey = "smartconnect.sourcemap.json";

    public async Task<FlowDispatchResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        await _ledgerWriter.AppendAsync(
            message,
            MessageLedgerStatus.Received,
            null,
            null,
            message.Payload.ToArray(),
            cancellationToken).ConfigureAwait(false);

        var flow = await _flows.GetByIdAsync(message.FlowId, cancellationToken).ConfigureAwait(false);
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
        var ctx = FlowExecutionContextFactory.Create(message, flow);
        var previousCtx = _contextAccessor?.Current;
        if (_contextAccessor is not null)
        {
            _contextAccessor.Current = ctx;
        }

        try
        {
            return await DispatchCoreAsync(message, flow, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (_contextAccessor is not null)
            {
                _contextAccessor.Current = previousCtx;
            }
        }
    }

    private async Task<FlowDispatchResult> DispatchCoreAsync(
        IntegrationMessage message,
        IntegrationFlow flow,
        FlowExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        // Source-side half: PreProcessor script, attachment extraction, route filters, source transforms.
        var (sourceMessage, shortCircuit) = await _sourceStages
            .ProcessAsync(message, flow, ctx, cancellationToken).ConfigureAwait(false);
        if (shortCircuit is not null)
        {
            return shortCircuit;
        }

        var workingMessage = sourceMessage!;
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
                var outcome = await _routeExecutor.ExecuteRouteAsync(workingMessage, routes[i], i, allowedRouteNames, ctx, useScopedLedger: false, cancellationToken).ConfigureAwait(false);
                list.Add(outcome);
                if (outcome.Failed)
                {
                    break;
                }
            }
            outcomes = [.. list];
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
                    () => _routeExecutor.ExecuteRouteAsync(workingMessage, route, ordinal, allowedRouteNames, ctx, useScopedLedger: true, cancellationToken),
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

        await _ledgerWriter.AppendAsync(
            workingMessage,
            MessageLedgerStatus.Completed,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        // PostProcessor script
        var overallSuccess = !anyOutboundFailed;
        if (_scriptExecutor is not null && !string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.PostProcessorScript))
        {
            await _scriptExecutor.RunPostProcessorAsync(
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
}
