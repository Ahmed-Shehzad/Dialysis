namespace Dialysis.Cds.Api.Controllers;

internal sealed record TreatmentSessionResponse(string SessionId, string? PatientMrn, IReadOnlyList<TreatmentObservationDto> Observations);
internal sealed record TreatmentObservationDto(string Code, string? Value, string? Unit);
internal sealed record PrescriptionByMrnResponse(decimal? BloodFlowRateMlMin, decimal? UfTargetVolumeMl, decimal? UfRateMlH);
