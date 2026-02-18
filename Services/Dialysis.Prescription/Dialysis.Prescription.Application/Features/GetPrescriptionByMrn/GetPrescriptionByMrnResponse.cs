namespace Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

/// <summary>
/// Prescription response - aligns with HL7 prescription OBX structure.
/// </summary>
public sealed record GetPrescriptionByMrnResponse(
    string OrderId,
    string TherapyModality,
    decimal? BloodFlowRateMlMin,
    decimal? UfTargetVolumeMl,
    decimal? UfRateMlH);
