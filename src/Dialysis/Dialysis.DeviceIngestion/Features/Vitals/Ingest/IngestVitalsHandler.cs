using Dialysis.Contracts.Events;
using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

public sealed class IngestVitalsHandler : ICommandHandler<IngestVitalsCommand, IngestVitalsResult>
{
    private readonly IObservationRepository _observationRepository;
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<IngestVitalsHandler> _logger;

    public IngestVitalsHandler(
        IObservationRepository observationRepository,
        IPublisher publisher,
        ITenantContext tenantContext,
        ILogger<IngestVitalsHandler> logger)
    {
        _observationRepository = observationRepository;
        _publisher = publisher;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IngestVitalsResult> HandleAsync(IngestVitalsCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var effective = request.Effective ?? ObservationEffective.UtcNow;
        var patientId = request.PatientId;

        ObservationId? firstId = null;

        if (request.BloodPressure is { } bp)
        {
            var observation = Observation.Create(
                tenantId,
                patientId,
                LoincCode.BloodPressure,
                bp.Display,
                UnitOfMeasure.MillimetersOfMercury,
                bp.Systolic > 0 ? bp.Systolic : bp.Diastolic,
                effective);

            await _observationRepository.AddAsync(observation, cancellationToken);
            firstId ??= new ObservationId(observation.Id.ToString());
            await PublishIntegrationEventsAsync(observation, cancellationToken);
        }

        if (request.HeartRate is { } hr)
        {
            var observation = Observation.Create(
                tenantId,
                patientId,
                LoincCode.HeartRate,
                $"Heart rate {hr} /min",
                UnitOfMeasure.BeatsPerMinute,
                hr,
                effective);

            await _observationRepository.AddAsync(observation, cancellationToken);
            firstId ??= new ObservationId(observation.Id.ToString());
            await PublishIntegrationEventsAsync(observation, cancellationToken);
        }

        if (request.WeightKg is { } weight)
        {
            var observation = Observation.Create(
                tenantId,
                patientId,
                LoincCode.BodyWeight,
                $"Body weight {weight} kg",
                UnitOfMeasure.Kilograms,
                weight,
                effective);

            await _observationRepository.AddAsync(observation, cancellationToken);
            firstId ??= new ObservationId(observation.Id.ToString());
            await PublishIntegrationEventsAsync(observation, cancellationToken);
        }

        return new IngestVitalsResult(firstId!);
    }

    private async Task PublishIntegrationEventsAsync(Observation observation, CancellationToken cancellationToken)
    {
        foreach (var evt in observation.IntegrationEvents)
        {
            await _publisher.PublishAsync(evt, cancellationToken);
        }
        observation.ClearIntegrationEvents();
    }
}
