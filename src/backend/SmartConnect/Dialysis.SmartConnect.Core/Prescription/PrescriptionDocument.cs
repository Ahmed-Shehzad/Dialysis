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
public sealed record BloodPumpSettings
{
    /// <summary>
    /// Blood pump channel (containment <c>1.1.3</c>) per IG §9.3.
    /// </summary>
    public BloodPumpSettings(int BloodFlowRateMlPerMin,
        string PumpMode)
    {
        this.BloodFlowRateMlPerMin = BloodFlowRateMlPerMin;
        this.PumpMode = PumpMode;
    }
    public int BloodFlowRateMlPerMin { get; init; }
    public string PumpMode { get; init; }
    public void Deconstruct(out int BloodFlowRateMlPerMin, out string PumpMode)
    {
        BloodFlowRateMlPerMin = this.BloodFlowRateMlPerMin;
        PumpMode = this.PumpMode;
    }
}

/// <summary>
/// Dialysate fluid channel (containment <c>1.1.4</c>) — used when modality is HD.
/// </summary>
public sealed record DialysateFluidSettings
{
    /// <summary>
    /// Dialysate fluid channel (containment <c>1.1.4</c>) — used when modality is HD.
    /// </summary>
    public DialysateFluidSettings(string FlowMode,
        int FlowRateMlPerMin,
        int VolumeLiters,
        string DialysateName)
    {
        this.FlowMode = FlowMode;
        this.FlowRateMlPerMin = FlowRateMlPerMin;
        this.VolumeLiters = VolumeLiters;
        this.DialysateName = DialysateName;
    }
    public string FlowMode { get; init; }
    public int FlowRateMlPerMin { get; init; }
    public int VolumeLiters { get; init; }
    public string DialysateName { get; init; }
    public void Deconstruct(out string FlowMode, out int FlowRateMlPerMin, out int VolumeLiters, out string DialysateName)
    {
        FlowMode = this.FlowMode;
        FlowRateMlPerMin = this.FlowRateMlPerMin;
        VolumeLiters = this.VolumeLiters;
        DialysateName = this.DialysateName;
    }
}

/// <summary>
/// UF (ultrafiltration) channel (containment <c>1.1.5</c>) — basic constant-weight
/// mode. Profile-driven UF (linear / exponential / step per IG §5.3) is a future slice.
/// </summary>
public sealed record UltrafiltrationSettings
{
    /// <summary>
    /// UF (ultrafiltration) channel (containment <c>1.1.5</c>) — basic constant-weight
    /// mode. Profile-driven UF (linear / exponential / step per IG §5.3) is a future slice.
    /// </summary>
    public UltrafiltrationSettings(string UfMode,
        int UfRateMlPerHour,
        int TargetVolumeToRemoveMl)
    {
        this.UfMode = UfMode;
        this.UfRateMlPerHour = UfRateMlPerHour;
        this.TargetVolumeToRemoveMl = TargetVolumeToRemoveMl;
    }
    public string UfMode { get; init; }
    public int UfRateMlPerHour { get; init; }
    public int TargetVolumeToRemoveMl { get; init; }
    public void Deconstruct(out string UfMode, out int UfRateMlPerHour, out int TargetVolumeToRemoveMl)
    {
        UfMode = this.UfMode;
        UfRateMlPerHour = this.UfRateMlPerHour;
        TargetVolumeToRemoveMl = this.TargetVolumeToRemoveMl;
    }
}

/// <summary>
/// The model the prescription resolver returns. Maps 1:1 to the OBX hierarchy in the
/// IG §5.4.2 sample. Profile-based therapies and HF / HDF wires are not yet modelled —
/// when modality &gt; HD, the responder emits the basic frame and logs a TODO so
/// integrators can spot the gap.
/// </summary>
public sealed record PrescriptionDocument
{
    /// <summary>
    /// The model the prescription resolver returns. Maps 1:1 to the OBX hierarchy in the
    /// IG §5.4.2 sample. Profile-based therapies and HF / HDF wires are not yet modelled —
    /// when modality &gt; HD, the responder emits the basic frame and logs a TODO so
    /// integrators can spot the gap.
    /// </summary>
    public PrescriptionDocument(string MedicalRecordNumber,
        string OrderNumber,
        string? OrderingProviderId,
        string? OrderingProviderFamily,
        string? OrderingProviderGiven,
        TherapyModality Modality,
        string TherapyCompletionMethod,
        BloodPumpSettings BloodPump,
        DialysateFluidSettings? Dialysate,
        UltrafiltrationSettings Ultrafiltration)
    {
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.OrderNumber = OrderNumber;
        this.OrderingProviderId = OrderingProviderId;
        this.OrderingProviderFamily = OrderingProviderFamily;
        this.OrderingProviderGiven = OrderingProviderGiven;
        this.Modality = Modality;
        this.TherapyCompletionMethod = TherapyCompletionMethod;
        this.BloodPump = BloodPump;
        this.Dialysate = Dialysate;
        this.Ultrafiltration = Ultrafiltration;
    }
    public string MedicalRecordNumber { get; init; }
    public string OrderNumber { get; init; }
    public string? OrderingProviderId { get; init; }
    public string? OrderingProviderFamily { get; init; }
    public string? OrderingProviderGiven { get; init; }
    public TherapyModality Modality { get; init; }
    public string TherapyCompletionMethod { get; init; }
    public BloodPumpSettings BloodPump { get; init; }
    public DialysateFluidSettings? Dialysate { get; init; }
    public UltrafiltrationSettings Ultrafiltration { get; init; }
    public void Deconstruct(out string MedicalRecordNumber, out string OrderNumber, out string? OrderingProviderId, out string? OrderingProviderFamily, out string? OrderingProviderGiven, out TherapyModality Modality, out string TherapyCompletionMethod, out BloodPumpSettings BloodPump, out DialysateFluidSettings? Dialysate, out UltrafiltrationSettings Ultrafiltration)
    {
        MedicalRecordNumber = this.MedicalRecordNumber;
        OrderNumber = this.OrderNumber;
        OrderingProviderId = this.OrderingProviderId;
        OrderingProviderFamily = this.OrderingProviderFamily;
        OrderingProviderGiven = this.OrderingProviderGiven;
        Modality = this.Modality;
        TherapyCompletionMethod = this.TherapyCompletionMethod;
        BloodPump = this.BloodPump;
        Dialysate = this.Dialysate;
        Ultrafiltration = this.Ultrafiltration;
    }
}
