namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Therapy modality from IG Table 2 — <c>MDC_HDIALY_MACH_TX_MODALITY</c>. The IG
/// covers <c>HD</c> (haemodialysis), <c>HF</c> (haemofiltration), and <c>HDF</c>
/// (haemodiafiltration). The basic prescription wire layout differs between HD (fluid
/// channel <c>1.1.4</c>) and HF (convective channel <c>1.1.4</c>); the responder
/// branches on this value.
/// </summary>
public enum TherapyModality
{
    Hd = 1,
    Hf = 2,
    Hdf = 3,
}

/// <summary>
/// Blood pump channel (containment <c>1.1.3</c>) per IG §9.3.
/// </summary>
public sealed record BloodPumpSettings(
    int BloodFlowRateMlPerMin,
    string PumpMode);

/// <summary>
/// Dialysate fluid channel (containment <c>1.1.4</c>) — used when modality is HD.
/// </summary>
public sealed record DialysateFluidSettings(
    string FlowMode,
    int FlowRateMlPerMin,
    int VolumeLiters,
    string DialysateName);

/// <summary>
/// UF (ultrafiltration) channel (containment <c>1.1.5</c>) — basic constant-weight
/// mode. Profile-driven UF (linear / exponential / step per IG §5.3) is a future slice.
/// </summary>
public sealed record UltrafiltrationSettings(
    string UfMode,
    int UfRateMlPerHour,
    int TargetVolumeToRemoveMl);

/// <summary>
/// The model the prescription resolver returns. Maps 1:1 to the OBX hierarchy in the
/// IG §5.4.2 sample. Profile-based therapies and HF / HDF wires are not yet modelled —
/// when modality &gt; HD, the responder emits the basic frame and logs a TODO so
/// integrators can spot the gap.
/// </summary>
public sealed record PrescriptionDocument(
    string MedicalRecordNumber,
    string OrderNumber,
    string? OrderingProviderId,
    string? OrderingProviderFamily,
    string? OrderingProviderGiven,
    TherapyModality Modality,
    string TherapyCompletionMethod,
    BloodPumpSettings BloodPump,
    DialysateFluidSettings? Dialysate,
    UltrafiltrationSettings Ultrafiltration);
