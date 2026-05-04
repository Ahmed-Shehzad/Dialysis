using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder;

internal sealed class TransponderRoutingSlipStarter(
    ITransponderSagaStore store,
    IMessageSerializer serializer,
    ITransponderBus bus,
    IOptions<TransponderRoutingSlipOptions> options,
    ILogger<TransponderRoutingSlipStarter> logger) : ITransponderRoutingSlipStarter
{
    public async Task<string> StartAsync(
        IReadOnlyList<TransponderRoutingSlipActivityRef> itinerary,
        TransponderPublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itinerary);
        if (itinerary.Count == 0)
            throw new ArgumentException("Routing slip itinerary must contain at least one activity.", nameof(itinerary));

        var catalog = options.Value.ActivitiesByName;
        foreach (var step in itinerary)
        {
            if (!catalog.ContainsKey(step.Name))
            {
                throw new InvalidOperationException(
                    $"Routing slip activity '{step.Name}' is not registered. Call AddRoutingSlipActivity for this name before starting a slip.");
            }
        }

        var slipId = Guid.NewGuid().ToString("N");
        var sagaKind = TransponderRoutingSlipPersistenceKind.SagaKind;
        var state = new TransponderRoutingSlipState
        {
            Itinerary = itinerary.Select(s => new TransponderRoutingSlipActivityRef { Name = s.Name, ArgumentsJson = s.ArgumentsJson }).ToList(),
            CurrentIndex = 0,
            CorrelationId = publishOptions?.CorrelationId,
        };

        var stateJson = StateJson(state);
        var insert = new TransponderSagaRecord
        {
            SagaKind = sagaKind,
            InstanceKey = slipId,
            StateName = state.Itinerary[0].Name,
            StateJson = stateJson,
            Version = 1,
            IsCompleted = false,
        };

        if (!await store.TryInsertAsync(insert, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException($"Routing slip insert race for slip id '{slipId}'.");

        logger.LogInformation("[RoutingSlip] Started slip {SlipId} with {Count} activities.", slipId, itinerary.Count);

        var continueMessage = new TransponderRoutingSlipContinue { SlipId = slipId, StepIndex = 0 };
        var dedup = $"{slipId}:step-0";
        await bus
            .PublishAsync(continueMessage, new TransponderPublishOptions(state.CorrelationId, dedup), cancellationToken)
            .ConfigureAwait(false);

        return slipId;
    }

    private string StateJson(TransponderRoutingSlipState state)
    {
        var bytes = serializer.Serialize(state);
        return Encoding.UTF8.GetString(bytes.Span);
    }
}
