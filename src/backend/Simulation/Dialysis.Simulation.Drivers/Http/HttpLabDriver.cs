using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.IntegrationEvents;

namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Drives the real Lab API and publishes the lab-result-received event (no result write surface).</summary>
public sealed class HttpLabDriver : ILabDriver
{
    private readonly HttpClient _client;
    private readonly ITransponderBus _bus;

    /// <summary>Creates the driver.</summary>
    public HttpLabDriver(HttpClient client, ITransponderBus bus)
    {
        _client = client;
        _bus = bus;
    }

    /// <inheritdoc />
    public async Task<PlacedLabOrder> PlaceLabOrderAsync(PlaceLabOrderCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/lab/orders",
            new
            {
                command.PatientId,
                Tests = command.Tests.Select(t => new { t.LoincCode, t.Display }),
                Priority = LabOrderPriority.Routine,
                command.Specimen,
            },
            context, cancellationToken).ConfigureAwait(false);

        var placer = await HttpDriverJson.GetStringPropAsync(
            _client, $"api/v1.0/lab/orders/{id}", "placerOrderNumber", context, cancellationToken).ConfigureAwait(false);
        return new PlacedLabOrder(id, placer);
    }

    /// <inheritdoc />
    public async Task<PublishedLabResult> PublishResultAsync(PublishLabResultCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        var observations = command.Observations
            .Select(o => new LabObservationContract(o.LoincCode, o.Display, o.Value, o.Unit, o.ReferenceRange, LabResultInterpretation.Normal))
            .ToList();

        var @event = new LabResultReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PlacerOrderNumber: command.PlacerOrderNumber,
            FillerOrderNumber: null,
            PatientId: command.PatientId,
            Status: LabOrderStatus.Resulted,
            Observations: observations,
            ResultedAtUtc: DateTime.UtcNow);

        await _bus.PublishAsync(@event, new TransponderPublishOptions(context.CorrelationId), cancellationToken).ConfigureAwait(false);
        return new PublishedLabResult(command.PlacerOrderNumber);
    }
}
