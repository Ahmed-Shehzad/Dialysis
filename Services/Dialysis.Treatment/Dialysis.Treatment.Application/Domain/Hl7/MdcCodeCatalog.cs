using System.Collections.Frozen;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Hl7;

/// <summary>
/// Static catalog of all IEEE 11073 MDC observation codes relevant to dialysis (PCD-01 Table 2).
/// Organized by IEEE 11073 containment hierarchy: MDS → VMD → Channel → Metric.
/// </summary>
public static class MdcCodeCatalog
{
    private static readonly FrozenDictionary<string, MdcCodeDescriptor> Catalog = BuildCatalog();

    private static FrozenDictionary<string, MdcCodeDescriptor> BuildCatalog()
    {
        var entries = new Dictionary<string, MdcCodeDescriptor>(StringComparer.OrdinalIgnoreCase);
        AddAll(entries);
        return entries.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryGet(string code, out MdcCodeDescriptor descriptor) =>
        Catalog.TryGetValue(code, out descriptor!);

    public static bool TryGet(ObservationCode code, out MdcCodeDescriptor descriptor) =>
        Catalog.TryGetValue(code.Value, out descriptor!);

    public static MdcCodeDescriptor? Get(string code) =>
        Catalog.GetValueOrDefault(code);

    public static IReadOnlyDictionary<string, MdcCodeDescriptor> All => Catalog;

    // ─── MDS level ───────────────────────────────────────────────────────────────

    private static readonly ContainmentLevel Mds = ContainmentLevel.Mds;
    private static readonly ContainmentLevel Vmd = ContainmentLevel.Vmd;
    private static readonly ContainmentLevel Chan = ContainmentLevel.Channel;
    private static readonly ContainmentLevel Metric = ContainmentLevel.Metric;

    private static void AddAll(Dictionary<string, MdcCodeDescriptor> d)
    {
        // ═══ MDS (Dialysis Machine) ══════════════════════════════════════════════
        Add(d, "MDC_DEV_SPEC_PROFILE_DIALYSIS", "Dialysis Machine MDS", Mds, null, null);

        // ═══ VMDs ════════════════════════════════════════════════════════════════
        Add(d, "MDC_DEV_SPEC_PROFILE_GENERAL", "General Parameters VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_PUMP", "Blood Pump VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_DIALYSATE", "Dialysate VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_HDF", "HDF VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_UF", "Ultrafiltration VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_ANTICOAG", "Anticoagulation VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_BP", "Blood Pressure VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_TEMP", "Temperature VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_ADEQUACY", "Adequacy VMD", Vmd, null, null);
        Add(d, "MDC_DEV_SPEC_PROFILE_ACCESS", "Vascular Access VMD", Vmd, null, null);

        // ═══ Channels ═══════════════════════════════════════════════════════════
        AddGeneralChannels(d);
        AddBloodPumpChannels(d);
        AddDialysateChannels(d);
        AddHdfChannels(d);
        AddUfChannels(d);
        AddAnticoagChannels(d);
        AddBpChannels(d);
        AddTempChannels(d);
        AddAdequacyChannels(d);
        AddAccessChannels(d);

        // ═══ Metrics ════════════════════════════════════════════════════════════
        AddGeneralMetrics(d);
        AddBloodPumpMetrics(d);
        AddDialysateMetrics(d);
        AddHdfMetrics(d);
        AddUfMetrics(d);
        AddAnticoagMetrics(d);
        AddBpMetrics(d);
        AddTempMetrics(d);
        AddAdequacyMetrics(d);
        AddAccessMetrics(d);
    }

    // ─── General VMD ─────────────────────────────────────────────────────────

    private static void AddGeneralChannels(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_CHAN_GENERAL", "General Channel", Chan, null, null);
        Add(d, "MDC_DIA_CHAN_THERAPY", "Therapy Channel", Chan, null, null);
        Add(d, "MDC_DIA_CHAN_WEIGHT", "Weight Channel", Chan, null, null);
    }

    private static void AddGeneralMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_MODE_OP", "Mode of Operation", Metric, null, null);
        Add(d, "MDC_DIA_MODALITY", "Treatment Modality", Metric, null, null);
        Add(d, "MDC_DIA_THERAPY_TIME_PRES", "Prescribed Therapy Time", Metric, "min", "min");
        Add(d, "MDC_DIA_THERAPY_TIME_REMAIN", "Remaining Therapy Time", Metric, "min", "min");
        Add(d, "MDC_DIA_THERAPY_TIME_ACTUAL", "Actual Therapy Time", Metric, "min", "min");
        Add(d, "MDC_DIA_COMPLETION_METHOD", "Completion Method", Metric, null, null);
        Add(d, "MDC_ATTR_TIME_ABS", "Absolute Machine Clock Time", Metric, null, null);
        Add(d, "MDC_DIA_WGT_PREDIAL", "Pre-Dialysis Weight", Metric, "kg", "kg");
        Add(d, "MDC_DIA_WGT_POSTDIAL", "Post-Dialysis Weight", Metric, "kg", "kg");
        Add(d, "MDC_DIA_WGT_TARGET", "Target Weight", Metric, "kg", "kg");
    }

    // ─── Blood Pump VMD ──────────────────────────────────────────────────────

    private static void AddBloodPumpChannels(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_CHAN_BLD_PUMP", "Blood Pump Channel", Chan, null, null);
        Add(d, "MDC_DIA_CHAN_BLD_PUMP_ART", "Arterial Pump Channel", Chan, null, null);
        Add(d, "MDC_DIA_CHAN_BLD_PUMP_VEN", "Venous Pump Channel", Chan, null, null);
    }

    private static void AddBloodPumpMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_BLD_PUMP_MODE", "Blood Pump Mode", Metric, null, null);
        Add(d, "MDC_DIA_BLD_FLOW_RATE_PRES", "Prescribed Blood Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_BLD_FLOW_RATE", "Actual Blood Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_BLD_VOL_PROCESSED", "Blood Volume Processed", Metric, "mL", "mL");
        Add(d, "MDC_DIA_BLD_PUMP_SPEED_SET", "Pump Speed Setting", Metric, "rpm", "{rpm}");
        Add(d, "MDC_DIA_BLD_PUMP_SPEED", "Actual Pump Speed", Metric, "rpm", "{rpm}");
        Add(d, "MDC_PRESS_BLD_ART", "Arterial Pressure", Metric, "mmHg", "mm[Hg]");
        Add(d, "MDC_PRESS_BLD_VEN", "Venous Pressure", Metric, "mmHg", "mm[Hg]");
        Add(d, "MDC_DIA_PRESS_TRANSMEMBRANE", "Transmembrane Pressure", Metric, "mmHg", "mm[Hg]");
    }

    // ─── Dialysate VMD ───────────────────────────────────────────────────────

    private static void AddDialysateChannels(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_CHAN_DIALYSATE", "Dialysate Channel", Chan, null, null);
        Add(d, "MDC_DIA_CHAN_DIALYSATE_COND", "Conductivity Channel", Chan, null, null);
    }

    private static void AddDialysateMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_DIALYSATE_FLOW_MODE", "Dialysate Flow Mode", Metric, null, null);
        Add(d, "MDC_DIA_DIALYSATE_FLOW_RATE_PRES", "Prescribed Dialysate Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_DIALYSATE_FLOW_RATE", "Actual Dialysate Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_DIALYSATE_PRESS", "Dialysate Pressure", Metric, "mmHg", "mm[Hg]");
        Add(d, "MDC_CONC_NA_PRES", "Prescribed Na Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_NA", "Actual Na Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_HCO3_PRES", "Prescribed Bicarbonate Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_HCO3", "Actual Bicarbonate Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_K_PRES", "Prescribed Potassium Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_K", "Actual Potassium Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_CA_PRES", "Prescribed Calcium Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_CONC_CA", "Actual Calcium Concentration", Metric, "mmol/L", "mmol/L");
        Add(d, "MDC_DIA_COND_TOTAL", "Total Conductivity", Metric, "mS/cm", "mS/cm");
        Add(d, "MDC_DIA_COND_TOTAL_PRES", "Prescribed Total Conductivity", Metric, "mS/cm", "mS/cm");
    }

    // ─── HDF VMD ─────────────────────────────────────────────────────────────

    private static void AddHdfChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_HDF", "HDF Channel", Chan, null, null);

    private static void AddHdfMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_HDF_FLOW_MODE", "Replacement Fluid Flow Mode", Metric, null, null);
        Add(d, "MDC_DIA_HDF_FLOW_RATE_PRES", "Prescribed Replacement Fluid Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_HDF_FLOW_RATE", "Actual Replacement Fluid Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_HDF_VOL_TOTAL", "Total Replacement Fluid Volume", Metric, "mL", "mL");
        Add(d, "MDC_DIA_HDF_LOCATION", "Replacement Fluid Location", Metric, null, null);
    }

    // ─── Ultrafiltration VMD ─────────────────────────────────────────────────

    private static void AddUfChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_UF", "UF Channel", Chan, null, null);

    private static void AddUfMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_UF_MODE", "UF Mode", Metric, null, null);
        Add(d, "MDC_DIA_UF_RATE_PRES", "Prescribed UF Rate", Metric, "mL/h", "mL/h");
        Add(d, "MDC_DIA_UF_RATE", "Actual UF Rate", Metric, "mL/h", "mL/h");
        Add(d, "MDC_DIA_UF_VOL_TARGET", "Target UF Volume", Metric, "mL", "mL");
        Add(d, "MDC_DIA_UF_VOL_TOTAL", "Total UF Volume Removed", Metric, "mL", "mL");
        Add(d, "MDC_DIA_UF_VOL_REMAIN", "Remaining UF Volume", Metric, "mL", "mL");
    }

    // ─── Anticoagulation VMD ─────────────────────────────────────────────────

    private static void AddAnticoagChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_ANTICOAG", "Anticoagulation Channel", Chan, null, null);

    private static void AddAnticoagMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_ANTICOAG_MODE", "Anticoagulation Mode", Metric, null, null);
        Add(d, "MDC_DIA_ANTICOAG_BOLUS_PRES", "Prescribed Bolus Dose", Metric, "IU", "[iU]");
        Add(d, "MDC_DIA_ANTICOAG_BOLUS", "Actual Bolus Dose", Metric, "IU", "[iU]");
        Add(d, "MDC_DIA_ANTICOAG_RATE_PRES", "Prescribed Continuous Rate", Metric, "IU/h", "[iU]/h");
        Add(d, "MDC_DIA_ANTICOAG_RATE", "Actual Continuous Rate", Metric, "IU/h", "[iU]/h");
        Add(d, "MDC_DIA_ANTICOAG_VOL_TOTAL", "Total Anticoagulant Administered", Metric, "IU", "[iU]");
        Add(d, "MDC_DIA_ANTICOAG_DRUG", "Anticoagulant Drug Name", Metric, null, null);
        Add(d, "MDC_DIA_ANTICOAG_STOP_TIME_BEFORE_END", "Stop Time Before End", Metric, "min", "min");
    }

    // ─── Blood Pressure VMD ──────────────────────────────────────────────────

    private static void AddBpChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_BP", "Blood Pressure Channel", Chan, null, null);

    private static void AddBpMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_PRESS_BLD_SYS", "Systolic Blood Pressure", Metric, "mmHg", "mm[Hg]");
        Add(d, "MDC_PRESS_BLD_DIA", "Diastolic Blood Pressure", Metric, "mmHg", "mm[Hg]");
        Add(d, "MDC_PRESS_BLD_MEAN", "Mean Arterial Pressure", Metric, "mmHg", "mm[Hg]");
        Add(d, "MDC_PULS_RATE", "Heart Rate / Pulse", Metric, "bpm", "/min");
    }

    // ─── Temperature VMD ─────────────────────────────────────────────────────

    private static void AddTempChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_TEMP", "Temperature Channel", Chan, null, null);

    private static void AddTempMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_TEMP_MODE", "Temperature Mode", Metric, null, null);
        Add(d, "MDC_DIA_TEMP_DIALYSATE_PRES", "Prescribed Dialysate Temperature", Metric, "°C", "Cel");
        Add(d, "MDC_DIA_TEMP_DIALYSATE", "Actual Dialysate Temperature", Metric, "°C", "Cel");
        Add(d, "MDC_TEMP_BLD", "Blood Temperature", Metric, "°C", "Cel");
        Add(d, "MDC_TEMP_BODY", "Body Temperature", Metric, "°C", "Cel");
    }

    // ─── Adequacy VMD ────────────────────────────────────────────────────────

    private static void AddAdequacyChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_ADEQUACY", "Adequacy Channel", Chan, null, null);

    private static void AddAdequacyMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_KTV_ONLINE", "Online Kt/V", Metric, null, null);
        Add(d, "MDC_DIA_KTV_PRES", "Prescribed Kt/V", Metric, null, null);
        Add(d, "MDC_DIA_CLEARANCE_UREA", "Urea Clearance", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_CLEARANCE_EFFECTIVE", "Effective Clearance", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_IONIC_DIALYSANCE", "Ionic Dialysance", Metric, "mL/min", "mL/min");
    }

    // ─── Vascular Access VMD ─────────────────────────────────────────────────

    private static void AddAccessChannels(Dictionary<string, MdcCodeDescriptor> d) => Add(d, "MDC_DIA_CHAN_ACCESS", "Vascular Access Channel", Chan, null, null);

    private static void AddAccessMetrics(Dictionary<string, MdcCodeDescriptor> d)
    {
        Add(d, "MDC_DIA_ACCESS_FLOW", "Access Flow Rate", Metric, "mL/min", "mL/min");
        Add(d, "MDC_DIA_ACCESS_RECIRC", "Recirculation Rate", Metric, "%", "%");
        Add(d, "MDC_DIA_BLD_LEAK_DETECT", "Blood Leak Detection", Metric, null, null);
    }

    private static void Add(
        Dictionary<string, MdcCodeDescriptor> d,
        string code,
        string displayName,
        ContainmentLevel level,
        string? defaultUnit,
        string? ucumUnit) =>
        d[code] = new MdcCodeDescriptor(new ObservationCode(code), displayName, level, defaultUnit, ucumUnit);
}
