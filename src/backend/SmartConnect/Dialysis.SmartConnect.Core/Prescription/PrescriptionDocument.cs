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
/// One step of a profile-driven UF programme (IG §5.3): hold <see cref="UfRateMlPerHour"/>
/// for <see cref="DurationMinutes"/>. For <c>LINEAR</c> / <c>EXP</c> shapes the steps are the
/// anchor points the machine interpolates between; for <c>STEP</c> they are literal plateaus.
/// </summary>
public sealed record UltrafiltrationProfileStep
{
    /// <summary>
    /// One step of a profile-driven UF programme (IG §5.3); see the type docs.
    /// </summary>
    public UltrafiltrationProfileStep(int DurationMinutes,
        int UfRateMlPerHour)
    {
        this.DurationMinutes = DurationMinutes;
        this.UfRateMlPerHour = UfRateMlPerHour;
    }
    public int DurationMinutes { get; init; }
    public int UfRateMlPerHour { get; init; }
    public void Deconstruct(out int DurationMinutes, out int UfRateMlPerHour)
    {
        DurationMinutes = this.DurationMinutes;
        UfRateMlPerHour = this.UfRateMlPerHour;
    }
}

/// <summary>
/// UF (ultrafiltration) channel (containment <c>1.1.5</c>). Constant modes
/// (<c>CONST-WT</c>/<c>CONST-WOT</c>) carry only the flat rate + target volume; profile-driven
/// modes (<c>PRO-WT</c>/<c>PRO-WOT</c> per IG §5.3) additionally carry
/// <see cref="ProfileShape"/> (<c>LINEAR</c>/<c>EXP</c>/<c>STEP</c>, protocol-neutral strings
/// like the other mode fields) and the <see cref="Profile"/> steps — emitted on the wire as
/// per-step observation rows, never a monolithic profile payload.
/// </summary>
public sealed record UltrafiltrationSettings
{
    /// <summary>
    /// UF (ultrafiltration) channel (containment <c>1.1.5</c>); see the type docs.
    /// </summary>
    public UltrafiltrationSettings(string UfMode,
        int UfRateMlPerHour,
        int TargetVolumeToRemoveMl,
        string? ProfileShape = null,
        IReadOnlyList<UltrafiltrationProfileStep>? Profile = null)
    {
        this.UfMode = UfMode;
        this.UfRateMlPerHour = UfRateMlPerHour;
        this.TargetVolumeToRemoveMl = TargetVolumeToRemoveMl;
        this.ProfileShape = ProfileShape;
        this.Profile = Profile;
    }
    public string UfMode { get; init; }
    public int UfRateMlPerHour { get; init; }
    public int TargetVolumeToRemoveMl { get; init; }
    public string? ProfileShape { get; init; }
    public IReadOnlyList<UltrafiltrationProfileStep>? Profile { get; init; }

    /// <summary><c>true</c> when <see cref="UfMode"/> is one of the profile-driven IG modes.</summary>
    public bool IsProfileMode => UfMode.StartsWith("PRO", StringComparison.OrdinalIgnoreCase);

    public void Deconstruct(out string UfMode, out int UfRateMlPerHour, out int TargetVolumeToRemoveMl, out string? ProfileShape, out IReadOnlyList<UltrafiltrationProfileStep>? Profile)
    {
        UfMode = this.UfMode;
        UfRateMlPerHour = this.UfRateMlPerHour;
        TargetVolumeToRemoveMl = this.TargetVolumeToRemoveMl;
        ProfileShape = this.ProfileShape;
        Profile = this.Profile;
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
/// exchange boundary (HIE); this model serves the device-side HL7v2 / 11073 family only —
/// the adopted 11073→FHIR mapping methodology (Riech et al. 2021, DOI 10.3205/mibe000222)
/// is documented in <c>docs/interoperability/ieee11073-to-fhir-mapping.md</c>.
/// Profile-driven UF (IG §5.3) rides on <see cref="UltrafiltrationSettings.Profile"/>.
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
    /// Profile-driven UF (IG §5.3) rides on <see cref="UltrafiltrationSettings.Profile"/>.
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
    /// or minimal is fine); HDF requires both. Also checks UF profile coherence (profile-driven
    /// modes need steps + a shape; constant modes must not carry profile data). Returns
    /// human-readable warnings instead of throwing — the responder always answers the machine
    /// with the channels that exist, and integrators surface the warnings through the channel
    /// ledger / logs.
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

        // UF profile consistency (IG §5.3) — same warn-don't-refuse contract as the
        // modality/channel checks above.
        if (Ultrafiltration.IsProfileMode)
        {
            if (Ultrafiltration.Profile is not { Count: > 0 })
                warnings.Add($"UF mode {Ultrafiltration.UfMode} is profile-driven but the prescription carries no profile steps.");
            if (string.IsNullOrWhiteSpace(Ultrafiltration.ProfileShape))
                warnings.Add($"UF mode {Ultrafiltration.UfMode} is profile-driven but no profile shape (LINEAR/EXP/STEP) is set.");
        }
        else if (Ultrafiltration.Profile is { Count: > 0 } || !string.IsNullOrWhiteSpace(Ultrafiltration.ProfileShape))
        {
            warnings.Add($"UF mode {Ultrafiltration.UfMode} is a constant mode but the prescription carries profile data — profiles belong to PRO-WT/PRO-WOT.");
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
