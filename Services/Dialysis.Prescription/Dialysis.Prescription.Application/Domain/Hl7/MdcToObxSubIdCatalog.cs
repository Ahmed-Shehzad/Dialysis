namespace Dialysis.Prescription.Application.Domain.Hl7;

/// <summary>
/// Maps MDC prescription codes to IEEE 11073 OBX-4 sub-ID (dotted notation).
/// Channel hierarchy: Machine 1.1.1, Anticoag 1.1.2, Blood Pump 1.1.3, Dialysate 1.1.4,
/// Filter 1.1.5, Convective 1.1.6, Safety 1.1.7, Outcomes 1.1.8, UF 1.1.9.
/// </summary>
public static class MdcToObxSubIdCatalog
{
    private static readonly IReadOnlyDictionary<string, string> CodeToSubId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["MDC_HDIALY_MACH_TIME"] = "1.1.1.1",
        ["MDC_HDIALY_MACH_MODE_OF_OPERATION"] = "1.1.1.2",
        ["MDC_HDIALY_MACH_TX_MODALITY"] = "1.1.1.3",
        ["MDC_HDIALY_MACH_THERAPY_TIME"] = "1.1.1.4",
        ["MDC_HDIALY_MACH_TIME_REMAIN"] = "1.1.1.5",
        ["MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING"] = "1.1.3.1",
        ["MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE"] = "1.1.3.2",
        ["MDC_HDIALY_BLD_PRESS_ART"] = "1.1.3.3",
        ["MDC_HDIALY_BLD_PUMP_PRESS_VEN"] = "1.1.3.4",
        ["MDC_HDIALY_BLD_PUMP_MODE"] = "1.1.3.5",
        ["MDC_HDIALY_UF_MODE"] = "1.1.9.1",
        ["MDC_HDIALY_UF_RATE_SETTING"] = "1.1.9.2",
        ["MDC_HDIALY_UF_RATE"] = "1.1.9.3",
        ["MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE"] = "1.1.9.4",
        ["MDC_HDIALY_UF_ACTUAL_REMOVED_VOL"] = "1.1.9.5",
        ["MDC_HDIALY_DIALYSATE_FLOW_MODE"] = "1.1.4.1",
        ["MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING"] = "1.1.4.2",
        ["MDC_HDIALY_DIALYSATE_CONC_NA_MODE"] = "1.1.4.3",
        ["MDC_HDIALY_DIALYSATE_CONC_NA_SETTING"] = "1.1.4.4",
        ["MDC_HDIALY_RF_FLOW_MODE"] = "1.1.6.1",
        ["MDC_HDIALY_RF_POST_FILTER_FLOW_RATE_SETTING"] = "1.1.6.2",
        ["MDC_HDIALY_RF_PRE_FILTER_FLOW_RATE_SETTING"] = "1.1.6.3",
        ["MDC_HDIALY_ANTICOAG_MODE"] = "1.1.2.1",
        ["MDC_HDIALY_ANTICOAG_INFUS_RATE_SETTING"] = "1.1.2.2",
    };

    public static string? Get(string code) =>
        CodeToSubId.TryGetValue(code ?? "", out string? subId) ? subId : null;

    public static string GetOrDefault(string code, string defaultSubId = "1.1.9.1") =>
        Get(code) ?? defaultSubId;
}
