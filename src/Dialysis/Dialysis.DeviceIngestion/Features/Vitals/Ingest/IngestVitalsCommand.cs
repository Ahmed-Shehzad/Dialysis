using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

/// <summary>
/// Command to ingest vitals and create FHIR Observations. Uses value objects.
/// </summary>
public sealed record IngestVitalsCommand(
    PatientId PatientId,
    BloodPressure? BloodPressure,
    decimal? HeartRate,
    decimal? WeightKg,
    ObservationEffective? Effective) : ICommand<IngestVitalsResult>;

public sealed record IngestVitalsResult(ObservationId FirstObservationId);
