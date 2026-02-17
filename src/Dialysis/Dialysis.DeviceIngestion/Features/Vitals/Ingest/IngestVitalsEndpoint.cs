using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

/// <summary>
/// Maps HTTP request to command and returns result. Vertical slice entry point.
/// Called from Gateway (Minimal API or controller).
/// </summary>
public static class IngestVitalsEndpoint
{
    public static async Task<IngestVitalsResponse> HandleAsync(
        IngestVitalsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        BloodPressure? bp = null;
        if (request.Systolic.HasValue || request.Diastolic.HasValue)
            bp = new BloodPressure(request.Systolic ?? 0, request.Diastolic ?? 0);

        ObservationEffective? effective = null;
        if (request.Timestamp.HasValue)
            effective = new ObservationEffective(request.Timestamp.Value);

        var patientId = new PatientId(request.PatientId);

        var command = new IngestVitalsCommand(
            patientId,
            bp,
            request.HeartRate,
            request.WeightKg,
            effective);

        var result = await sender.SendAsync(command, cancellationToken);
        return new IngestVitalsResponse(result.FirstObservationId.Value);
    }
}

public sealed record IngestVitalsResponse(string ObservationId);
