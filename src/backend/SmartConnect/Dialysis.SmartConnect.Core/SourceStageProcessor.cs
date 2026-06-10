using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;

namespace Dialysis.SmartConnect;

/// <summary>
/// Runs the source-side half of a flow dispatch for the runtime engine: PreProcessor script,
/// attachment extraction, route filters, and source-side transform stages — everything that
/// happens before the outbound route loop.
/// </summary>
internal sealed class SourceStageProcessor
{
    private readonly IFlowPluginRegistry _plugins;
    private readonly FlowLedgerWriter _ledgerWriter;
    private readonly ChannelScriptExecutor? _scriptExecutor;
    private readonly AttachmentExtractionPipeline? _attachmentExtraction;

    /// <summary>
    /// Runs the source-side half of a flow dispatch for the runtime engine: PreProcessor script,
    /// attachment extraction, route filters, and source-side transform stages — everything that
    /// happens before the outbound route loop.
    /// </summary>
    public SourceStageProcessor(
        IFlowPluginRegistry plugins,
        FlowLedgerWriter ledgerWriter,
        ChannelScriptExecutor? scriptExecutor,
        AttachmentExtractionPipeline? attachmentExtraction)
    {
        _plugins = plugins;
        _ledgerWriter = ledgerWriter;
        _scriptExecutor = scriptExecutor;
        _attachmentExtraction = attachmentExtraction;
    }

    /// <summary>
    /// Processes the source-side stages and returns either the working message the route loop
    /// should dispatch, or a short-circuit <see cref="FlowDispatchResult"/> (filter drop or
    /// unregistered-plugin failure) that ends the dispatch here.
    /// </summary>
    public async Task<(IntegrationMessage? Message, FlowDispatchResult? ShortCircuit)> ProcessAsync(
        IntegrationMessage message,
        IntegrationFlow flow,
        FlowExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        // PreProcessor script
        var workingMessage = message;
        if (_scriptExecutor is not null && !string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.PreProcessorScript))
        {
            var preResult = await _scriptExecutor.RunPreProcessorAsync(
                flow.Pipeline.Scripts!.PreProcessorScript!, workingMessage, cancellationToken).ConfigureAwait(false);
            if (preResult.Dropped)
            {
                await _ledgerWriter.AppendAsync(workingMessage, MessageLedgerStatus.RouteFilterDropped, null, "PreProcessor", null, cancellationToken).ConfigureAwait(false);
                return (null, new FlowDispatchResult { Succeeded = true, Error = null, OutboundRoutesAttempted = [] });
            }

            if (preResult.NewPayload is not null)
            {
                workingMessage = workingMessage.CloneWithPayload(preResult.NewPayload);
            }
        }

        // Attachment Handler — runs once between PreProcessor and route filters. Extracts inline bulky
        // content into the attachment store and rewrites the payload with ${ATTACH:<id>} tokens.
        if (_attachmentExtraction is not null && flow.Pipeline.AttachmentHandler is not null)
        {
            var rewritten = await _attachmentExtraction
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
            return (null, filterOutcome);
        }

        // Source-side transform stages (Mirth source-connector transformer steps; used for Destination Set Filter).
        ctx.SetCurrentStageContext(CodeTemplateContext.SourceTransformer);
        foreach (var stageSlot in flow.Pipeline.SourceTransformStages)
        {
            var stage = _plugins.TryResolveTransformStage(stageSlot.Kind);
            if (stage is null)
            {
                return (null, Failure($"Source transform stage kind '{stageSlot.Kind}' is not registered."));
            }

            var workingForStage = string.IsNullOrWhiteSpace(stageSlot.ParametersJson)
                ? workingMessage
                : workingMessage.WithMetadata("smartconnect.transform.parameters", stageSlot.ParametersJson!);
            workingMessage = await stage.TransformAsync(workingForStage, cancellationToken).ConfigureAwait(false);
        }

        return (workingMessage, null);
    }

    private async Task<FlowDispatchResult?> RunRouteFiltersAsync(
        IntegrationFlow flow,
        IntegrationMessage message,
        CancellationToken cancellationToken)
    {
        foreach (var slot in flow.Pipeline.RouteFilters)
        {
            var filter = _plugins.TryResolveRouteFilter(slot.Kind);
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

            await _ledgerWriter.AppendAsync(
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

    private static FlowDispatchResult Failure(string error) =>
        new() { Succeeded = false, Error = error, OutboundRoutesAttempted = [] };
}
