namespace Dialysis.Cds.Api.Controllers;

public sealed record TreatmentSessionResponse(string SessionId, string? PatientMrn, IReadOnlyList<TreatmentObservationDto> Observations);
public sealed record TreatmentObservationDto(string Code, string? Value, string? Unit);
public sealed record PrescriptionByMrnResponse(decimal? BloodFlowRateMlMin, decimal? UfTargetVolumeMl, decimal? UfRateMlH);
