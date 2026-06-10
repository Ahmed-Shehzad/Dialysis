using System.Text;
using Dialysis.BuildingBlocks.Transponder.Sagas;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

internal sealed class TransponderRoutingSlipContinueConsumer : IConsumer<TransponderRoutingSlipContinue>
{
    private readonly ITransponderSagaStore _store;
    private readonly IMessageSerializer _serializer;
    private readonly IOptions<TransponderRoutingSlipOptions> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly TransponderRoutingSlipEventPublisher _events;
    private readonly ILogger<TransponderRoutingSlipContinueConsumer> _logger;
    public TransponderRoutingSlipContinueConsumer(ITransponderSagaStore store,
        IMessageSerializer serializer,
        IOptions<TransponderRoutingSlipOptions> options,
        IServiceProvider serviceProvider,
        TransponderRoutingSlipEventPublisher events,
        ILogger<TransponderRoutingSlipContinueConsumer> logger)
    {
        _store = store;
        _serializer = serializer;
        _options = options;
        _serviceProvider = serviceProvider;
        _events = events;
        _logger = logger;
    }
    private static readonly string _sagaKind = TransponderRoutingSlipPersistenceKind.SagaKind;

    // The gotos below all jump forward to the DispatchPending epilogue — publish whatever was
    // queued, then leave. A flag-plus-nested-if rewrite of this saga step machine would bury
    // that control flow; the jump target is single and forward-only, so goto stays.
#pragma warning disable S907
    public async Task HandleAsync(ConsumeContext<TransponderRoutingSlipContinue> context)
    {
        var slipId = context.Message.SlipId;
        ArgumentException.ThrowIfNullOrWhiteSpace(slipId);

        var gate = TransponderSagaInstanceLock.Get(_sagaKind, slipId);
        TransponderRoutingSlipContinue? followUp = null;
        TransponderPublishOptions? followUpOptions = null;
        var pending = new List<Func<Task>>();
        await gate.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            var record = await _store.GetAsync(_sagaKind, slipId, context.CancellationToken).ConfigureAwait(false);
            if (record is null)
            {
                _logger.LogWarning("[RoutingSlip] No persisted slip for {SlipId}; ignoring continue.", slipId);
                return;
            }

            if (record.IsCompleted)
            {
                _logger.LogDebug("[RoutingSlip] Slip {SlipId} already completed; ignoring continue.", slipId);
                return;
            }

            var state = DeserializeState(record.StateJson);
            if (state is null)
            {
                _logger.LogWarning("[RoutingSlip] Slip {SlipId} has empty state; ignoring continue.", slipId);
                return;
            }

            NormalizeState(state);

            if (context.Message.StepIndex != state.CurrentIndex)
            {
                _logger.LogDebug(
                    "[RoutingSlip] Slip {SlipId} continue step {StepIndex} does not match current index {CurrentIndex}; treating as duplicate.",
                    slipId,
                    context.Message.StepIndex,
                    state.CurrentIndex);
                return;
            }

            if (state.CurrentIndex >= state.Itinerary.Count)
            {
                await _store.DeleteAsync(_sagaKind, slipId, context.CancellationToken).ConfigureAwait(false);
                return;
            }

            var step = state.Itinerary[state.CurrentIndex];
            if (!_options.Value.ActivitiesByName.TryGetValue(step.Name, out var activityType))
            {
                _logger.LogError("[RoutingSlip] Unknown activity '{Activity}' for slip {SlipId}.", step.Name, slipId);
                await _store.DeleteAsync(_sagaKind, slipId, context.CancellationToken).ConfigureAwait(false);
                var idx = state.CurrentIndex;
                var corr = state.CorrelationId;
                pending.Add(() =>
                    _events.PublishActivityFaultedAsync(
                        slipId,
                        step.Name,
                        idx,
                        step.ArgumentsJson,
                        "Activity is not registered.",
                        null,
                        corr,
                        context.CancellationToken));
                pending.Add(() =>
                    _events.PublishSlipFaultedAsync(
                        slipId,
                        $"Routing slip faulted: activity '{step.Name}' is not registered.",
                        null,
                        step.Name,
                        idx,
                        corr,
                        context.CancellationToken));
                goto DispatchPending;
            }

            var activity = (IRoutingSlipActivity)ActivatorUtilities.CreateInstance(_serviceProvider, activityType);
            var activityContext = new RoutingSlipActivityExecutionContext(slipId, context, context.Bus, step, state.Variables);

            try
            {
                await activity.ExecuteAsync(activityContext, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var snapshot = state.CompletedActivities.ToList();
                var variablesCopy = new Dictionary<string, string>(state.Variables, StringComparer.Ordinal);
                var failedIndex = state.CurrentIndex;
                var corr = state.CorrelationId;
                await _store.DeleteAsync(_sagaKind, slipId, context.CancellationToken).ConfigureAwait(false);
                pending.Add(() =>
                    PublishExecuteFaultAndCompensationAsync(
                        slipId,
                        step.Name,
                        failedIndex,
                        step.ArgumentsJson,
                        ex,
                        snapshot,
                        variablesCopy,
                        corr,
                        context,
                        context.CancellationToken));
                goto DispatchPending;
            }

            var completedIndex = state.CurrentIndex;
            state.CompletedActivities.Add(
                new TransponderRoutingSlipCompletedActivityEntry
                {
                    Index = completedIndex,
                    Name = step.Name,
                    ArgumentsJson = step.ArgumentsJson,
                });
            state.CurrentIndex++;
            var version = record.Version;
            var correlationId = state.CorrelationId;

            if (state.CurrentIndex >= state.Itinerary.Count)
            {
                await _store.DeleteAsync(_sagaKind, slipId, context.CancellationToken).ConfigureAwait(false);
                pending.Add(() =>
                    _events.PublishActivityCompletedAsync(
                        slipId,
                        completedIndex,
                        step.Name,
                        step.ArgumentsJson,
                        correlationId,
                        context.CancellationToken));
                pending.Add(() => _events.PublishSlipCompletedAsync(slipId, correlationId, context.CancellationToken));
                _logger.LogInformation("[RoutingSlip] Completed slip {SlipId}.", slipId);
                goto DispatchPending;
            }

            var next = new TransponderSagaRecord
            {
                SagaKind = _sagaKind,
                InstanceKey = slipId,
                StateName = state.Itinerary[state.CurrentIndex].Name,
                StateJson = SerializeState(state),
                Version = version + 1,
                IsCompleted = false,
            };

            if (!await _store.TryUpdateAsync(next, version, context.CancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException($"[RoutingSlip] Concurrency conflict for slip '{slipId}' (expected version {version}).");

            pending.Add(() =>
                _events.PublishActivityCompletedAsync(
                    slipId,
                    completedIndex,
                    step.Name,
                    step.ArgumentsJson,
                    correlationId,
                    context.CancellationToken));

            followUp = new TransponderRoutingSlipContinue { SlipId = slipId, StepIndex = state.CurrentIndex };
            followUpOptions = new TransponderPublishOptions(state.CorrelationId, $"{slipId}:step-{state.CurrentIndex}");
        }
        finally
        {
            gate.Release();
        }

    DispatchPending:
        foreach (var publish in pending)
            await publish().ConfigureAwait(false);

        if (followUp is not null && followUpOptions is not null)
        {
            await context.Bus
                .PublishAsync(followUp, followUpOptions.Value, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }
#pragma warning restore S907

    private async Task PublishExecuteFaultAndCompensationAsync(
        string trackingNumber,
        string failedActivityName,
        int failedActivityIndex,
        string? failedArgumentsJson,
        Exception exception,
        IReadOnlyList<TransponderRoutingSlipCompletedActivityEntry> completedSnapshot,
        Dictionary<string, string> variables,
        string? correlationId,
        ConsumeContext<TransponderRoutingSlipContinue> context,
        CancellationToken cancellationToken)
    {
        await _events
            .PublishActivityFaultedAsync(
                trackingNumber,
                failedActivityName,
                failedActivityIndex,
                failedArgumentsJson,
                exception.Message,
                exception.ToString(),
                correlationId,
                cancellationToken)
            .ConfigureAwait(false);

        var compensationFailed = false;
        string? lastFailedName = null;
        int? lastFailedIndex = null;

        foreach (var entry in completedSnapshot.OrderByDescending(static e => e.Index))
        {
            if (!_options.Value.ActivitiesByName.TryGetValue(entry.Name, out var activityType))
                continue;

            var instance = ActivatorUtilities.CreateInstance(_serviceProvider, activityType);
            if (instance is not IRoutingSlipCompensatableActivity compensatable)
                continue;

            try
            {
                var compCtx = new RoutingSlipActivityCompensationExecutionContext(
                    trackingNumber,
                    context,
                    context.Bus,
                    entry,
                    variables);
                await compensatable.CompensateAsync(compCtx, cancellationToken).ConfigureAwait(false);
                await _events
                    .PublishActivityCompensatedAsync(trackingNumber, entry.Name, entry.Index, correlationId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception compEx)
            {
                compensationFailed = true;
                lastFailedName = entry.Name;
                lastFailedIndex = entry.Index;
                await _events
                    .PublishActivityCompensationFailedAsync(
                        trackingNumber,
                        entry.Name,
                        entry.Index,
                        compEx.Message,
                        compEx.ToString(),
                        correlationId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (compensationFailed)
        {
            await _events
                .PublishSlipCompensationFailedAsync(
                    trackingNumber,
                    "One or more compensation steps failed.",
                    lastFailedName,
                    lastFailedIndex,
                    correlationId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await _events
            .PublishSlipFaultedAsync(
                trackingNumber,
                $"Activity '{failedActivityName}' faulted: {exception.Message}",
                exception.ToString(),
                failedActivityName,
                failedActivityIndex,
                correlationId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static void NormalizeState(TransponderRoutingSlipState state) =>
        state.CompletedActivities ??= [];

    private TransponderRoutingSlipState? DeserializeState(string? stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
            return null;
        var bytes = Encoding.UTF8.GetBytes(stateJson);
        var deserialized = _serializer.Deserialize(typeof(TransponderRoutingSlipState), bytes.AsMemory()) as TransponderRoutingSlipState;
        if (deserialized is not null)
            NormalizeState(deserialized);
        return deserialized;
    }

    private string SerializeState(TransponderRoutingSlipState state)
    {
        NormalizeState(state);
        var bytes = _serializer.Serialize(state);
        return Encoding.UTF8.GetString(bytes.Span);
    }
}
