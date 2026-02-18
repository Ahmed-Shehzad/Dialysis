namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Groups all parameters needed to map a Prescription to FHIR ServiceRequest.
/// Aligns with Prescription service domain and HL7 ORC + OBX segments.
/// </summary>
public sealed record PrescriptionMappingInput(
    string OrderId,
    string PatientMrn,
    string? Modality,
    string? OrderingProvider,
    decimal? BloodFlowRateMlMin,
    decimal? UfRateMlH,
    decimal? UfTargetVolumeMl,
    DateTimeOffset? ReceivedAt);
