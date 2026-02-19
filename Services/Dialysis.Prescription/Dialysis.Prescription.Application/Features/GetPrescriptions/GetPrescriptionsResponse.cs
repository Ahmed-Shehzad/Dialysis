namespace Dialysis.Prescription.Application.Features.GetPrescriptions;

public sealed record GetPrescriptionsResponse(IReadOnlyList<PrescriptionSummary> Prescriptions);

public sealed record PrescriptionSummary(
    string OrderId,
    string PatientMrn,
    string? Modality,
    string? OrderingProvider,
    decimal? BloodFlowRateMlMin,
    decimal? UfRateMlH,
    decimal? UfTargetVolumeMl,
    DateTimeOffset? ReceivedAt);
