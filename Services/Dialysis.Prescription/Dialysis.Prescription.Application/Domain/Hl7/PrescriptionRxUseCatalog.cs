using System.Collections.Frozen;

using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Application.Domain.Hl7;

/// <summary>
/// Maps prescription MDC codes to Table 2 Use column (M/C/O).
/// Prescription-eligible parameters are M or C.
/// </summary>
public static class PrescriptionRxUseCatalog
{
    private static readonly FrozenDictionary<string, RxUse> Catalog = BuildCatalog();

    private static FrozenDictionary<string, RxUse> BuildCatalog()
    {
        var d = new Dictionary<string, RxUse>(StringComparer.OrdinalIgnoreCase);
        // Machine channel (1.1.1)
        d["MDC_HDIALY_MACH_TIME"] = RxUse.M;
        d["MDC_HDIALY_MACH_MODE_DESCRIPTION"] = RxUse.O;
        d["MDC_HDIALY_MACH_MODE_OF_OPERATION"] = RxUse.M;
        d["MDC_TIME_PD_MAINTENANCE_TO_NEXT_SERVICE"] = RxUse.O;
        d["MDC_MAINTENANCE_NEXT_SERVICE_DATE"] = RxUse.O;
        d["MDC_HDIALY_MACH_MAINT_TX_REMAIN"] = RxUse.O;
        d["MDC_HDIALY_MACH_BLD_PUMP_ON"] = RxUse.M;
        d["MDC_HDIALY_MACH_TX_FLUID_BYPASS"] = RxUse.M;
        d["MDC_HDIALY_MACH_TX_MODALITY"] = RxUse.M;
        d["MDC_HDIALY_MACH_THERAPY_TIME"] = RxUse.M;
        d["MDC_HDIALY_MACH_TIME_REMAIN"] = RxUse.C;
        d["MDC_TEMP_ROOM"] = RxUse.O;
        // Blood Pump channel (1.1.3)
        d["MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING"] = RxUse.M;
        d["MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE"] = RxUse.O;
        d["MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_MEAN"] = RxUse.O;
        d["MDC_HDIALY_BLD_PRESS_ART"] = RxUse.M;
        d["MDC_HDIALY_BLD_PUMP_MODE"] = RxUse.M;
        d["MDC_EVT_HDIALY_BLD_PUMP_STOP"] = RxUse.M;
        d["MDC_HDIALY_BLD_PUMP_PRESS_VEN"] = RxUse.M;
        d["MDC_HDIALY_BLOOD_TEMP_VEN"] = RxUse.O;
        d["MDC_HDIALY_BLD_PUMP_BLOOD_PROCESSED_TOTAL"] = RxUse.O;
        // UF channel (1.1.9)
        d["MDC_HDIALY_UF_MODE"] = RxUse.M;
        d["MDC_HDIALY_UF_RATE_SETTING"] = RxUse.M;
        d["MDC_HDIALY_UF_RATE"] = RxUse.M;
        d["MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE"] = RxUse.C;
        d["MDC_HDIALY_UF_ACTUAL_REMOVED_VOL"] = RxUse.M;
        d["MDC_EVT_HDIALY_UF_RATE_RANGE"] = RxUse.M;
        d["MDC_HDIALY_PROFILE"] = RxUse.C;
        // Pumpable profile mapping (§3.2.6)
        d["MDC_HDIALY_DIALYSATE_FLOW_MODE"] = RxUse.M;
        d["MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING"] = RxUse.M;
        d["MDC_HDIALY_RF_FLOW_MODE"] = RxUse.M;
        d["MDC_HDIALY_RF_POST_FILTER_FLOW_RATE_SETTING"] = RxUse.M;
        d["MDC_HDIALY_RF_PRE_FILTER_FLOW_RATE_SETTING"] = RxUse.M;
        d["MDC_HDIALY_ANTICOAG_MODE"] = RxUse.M;
        d["MDC_HDIALY_ANTICOAG_INFUS_RATE_SETTING"] = RxUse.M;
        d["MDC_HDIALY_DIALYSATE_CONC_NA_MODE"] = RxUse.M;
        d["MDC_HDIALY_DIALYSATE_CONC_NA_SETTING"] = RxUse.M;
        return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryGet(string code, out RxUse use) => Catalog.TryGetValue(code, out use);

    public static RxUse? Get(string code) => Catalog.TryGetValue(code, out RxUse use) ? use : null;
}
