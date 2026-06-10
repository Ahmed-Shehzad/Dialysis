namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Therapy modality from IG Table 2 — <c>MDC_HDIALY_MACH_TX_MODALITY</c>. The IG
/// covers <c>HD</c> (haemodialysis), <c>HF</c> (haemofiltration), and <c>HDF</c>
/// (haemodiafiltration). Channel expectations per modality:
/// <list type="bullet">
///   <item><c>HD</c> — diffusive: dialysate fluid channel required, no substitution fluid.</item>
///   <item><c>HF</c> — primarily convective: substitution/replacement fluid required;
///     dialysate is absent or minimal (the model allows a minimal dialysate section).</item>
///   <item><c>HDF</c> — diffusive + convective: both dialysate <b>and</b> substitution
///     fluid required.</item>
/// </list>
/// The model stays protocol-neutral; each channel is mapped outward independently as
/// IEEE 11073 MDC-coded observation rows (see <see cref="Hl7V2RxResponseBuilder"/>) rather
/// than one monolithic per-modality payload. Consistency between modality and the channels
/// actually present is surfaced via
/// <see cref="PrescriptionDocument.GetModalityConsistencyWarnings"/> — warnings, not
/// refusals, so a machine query is always answered with whatever the prescription carries.
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
/// Substitution / replacement fluid channel — the convective leg of HF and HDF. Absent
/// for plain HD. <c>DilutionMode</c> is the infusion point relative to the filter
/// (<c>PRE</c> / <c>POST</c> per IG conventions, kept as a string like the other mode
/// fields so the model stays protocol-neutral).
/// </summary>
public sealed record SubstitutionFluidSettings
{
    /// <summary>
    /// Substitution / replacement fluid channel — the convective leg of HF and HDF. Absent
    /// for plain HD. <c>DilutionMode</c> is the infusion point relative to the filter
    /// (<c>PRE</c> / <c>POST</c> per IG conventions, kept as a string like the other mode
    /// fields so the model stays protocol-neutral).
    /// </summary>
    public SubstitutionFluidSettings(string DilutionMode,
        int FlowRateMlPerMin,
        int TargetVolumeMl,
        string FluidName)
    {
        this.DilutionMode = DilutionMode;
        this.FlowRateMlPerMin = FlowRateMlPerMin;
        this.TargetVolumeMl = TargetVolumeMl;
        this.FluidName = FluidName;
    }
    public string DilutionMode { get; init; }
    public int FlowRateMlPerMin { get; init; }
    public int TargetVolumeMl { get; init; }
    public string FluidName { get; init; }
    public void Deconstruct(out string DilutionMode, out int FlowRateMlPerMin, out int TargetVolumeMl, out string FluidName)
    {
        DilutionMode = this.DilutionMode;
        FlowRateMlPerMin = this.FlowRateMlPerMin;
        TargetVolumeMl = this.TargetVolumeMl;
        FluidName = this.FluidName;
    }
}

/// <summary>
/// The model the prescription resolver returns. Protocol-neutral: each channel section is
/// optional and maps outward independently as IEEE 11073 MDC-coded observation rows (the
/// OBX hierarchy of IG §5.4.2) — HF and HDF are expressed through which channels are
/// present (HF: substitution fluid, dialysate absent/minimal; HDF: dialysate + substitution
/// fluid), not through per-modality payload shapes. FHIR remains the clinical-system
/// exchange boundary (HIE); this model serves the device-side HL7v2 / 11073 family only.
/// Profile-driven UF (linear / exponential / step per IG §5.3) is still a future slice.
/// </summary>
public sealed record PrescriptionDocument
{
    /// <summary>
    /// The model the prescription resolver returns. Protocol-neutral: each channel section is
    /// optional and maps outward independently as IEEE 11073 MDC-coded observation rows (the
    /// OBX hierarchy of IG §5.4.2) — HF and HDF are expressed through which channels are
    /// present (HF: substitution fluid, dialysate absent/minimal; HDF: dialysate + substitution
    /// fluid), not through per-modality payload shapes. FHIR remains the clinical-system
    /// exchange boundary (HIE); this model serves the device-side HL7v2 / 11073 family only.
    /// Profile-driven UF (linear / exponential / step per IG §5.3) is still a future slice.
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
        UltrafiltrationSettings Ultrafiltration,
        SubstitutionFluidSettings? SubstitutionFluid = null)
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
        this.SubstitutionFluid = SubstitutionFluid;
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
    public SubstitutionFluidSettings? SubstitutionFluid { get; init; }

    /// <summary>
    /// Modality ↔ channel consistency check: HD expects dialysate and no substitution fluid;
    /// HF requires substitution fluid (dialysate optional — primarily convective, so absent
    /// or minimal is fine); HDF requires both. Returns human-readable warnings instead of
    /// throwing — the responder always answers the machine with the channels that exist,
    /// and integrators surface the warnings through the channel ledger / logs.
    /// </summary>
    public IReadOnlyList<string> GetModalityConsistencyWarnings()
    {
        var warnings = new List<string>();
        switch (Modality)
        {
            case TherapyModality.Hd:
                if (Dialysate is null)
                    warnings.Add("HD prescription has no dialysate fluid channel — diffusive therapy needs one.");
                if (SubstitutionFluid is not null)
                    warnings.Add("HD prescription carries a substitution fluid channel — substitution fluid belongs to HF/HDF.");
                break;
            case TherapyModality.Hf:
                if (SubstitutionFluid is null)
                    warnings.Add("HF prescription has no substitution fluid channel — the convective dose is undefined without one.");
                break;
            case TherapyModality.Hdf:
                if (Dialysate is null)
                    warnings.Add("HDF prescription has no dialysate fluid channel — HDF needs both dialysate and substitution fluid.");
                if (SubstitutionFluid is null)
                    warnings.Add("HDF prescription has no substitution fluid channel — HDF needs both dialysate and substitution fluid.");
                break;
            default:
                break;
        }
        return warnings;
    }

    public void Deconstruct(out string MedicalRecordNumber, out string OrderNumber, out string? OrderingProviderId, out string? OrderingProviderFamily, out string? OrderingProviderGiven, out TherapyModality Modality, out string TherapyCompletionMethod, out BloodPumpSettings BloodPump, out DialysateFluidSettings? Dialysate, out UltrafiltrationSettings Ultrafiltration, out SubstitutionFluidSettings? SubstitutionFluid)
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
        SubstitutionFluid = this.SubstitutionFluid;
    }
}
