import type { ObservationDto } from "../types";

/** Objects with at least code/value for observation lookup. */
type ObsLike = Pick<ObservationDto, "code" | "value"> & Partial<ObservationDto>;

function extractMdcCode(code: string | undefined): string {
    if (!code) return "";
    const part = code.split("^")[1];
    return part ?? code;
}

export function findObs(
    observations: ObsLike[],
    codeList: readonly string[]
): ObsLike | undefined {
    return observations.find((o) => {
        const c = extractMdcCode(o.code);
        return codeList.includes(c);
    });
}

export function getObsValue(
    observations: ObsLike[],
    codeList: readonly string[]
): string | undefined {
    return findObs(observations, codeList)?.value;
}

/** MDC codes for common dialysis observations (IEEE 11073). */
export const MDC = {
    WGT_PREDIAL: ["MDC_DIA_WGT_PREDIAL"],
    WGT_POSTDIAL: ["MDC_DIA_WGT_POSTDIAL"],
    WGT_TARGET: ["MDC_DIA_WGT_TARGET"],
    BP_SYS: ["MDC_PRESS_BLD_SYS"],
    BP_DIA: ["MDC_PRESS_BLD_DIA"],
    HEART_RATE: ["MDC_PULS_RATE"],
    UF_TARGET: [
        "MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE",
        "MDC_HDIALY_UF_TARGET_VOL",
        "MDC_DIA_UF_VOL_TARGET",
    ],
    UF_ACTUAL: [
        "MDC_HDIALY_UF_ACTUAL_REMOVED_VOL",
        "MDC_DIA_UF_VOL_TOTAL",
    ],
    UF_RATE: ["MDC_HDIALY_UF_RATE", "MDC_DIA_UF_RATE"],
    BLOOD_FLOW: [
        "MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE",
        "MDC_DIA_BLD_FLOW_RATE",
    ],
    DIALYSATE_FLOW: [
        "MDC_HDIALY_DIALYSATE_FLOW_RATE",
        "MDC_DIA_DIALYSATE_FLOW_RATE",
    ],
    TMP: ["MDC_DIA_PRESS_TRANSMEMBRANE"],
    THERAPY_TIME_REMAIN: [
        "MDC_HDIALY_MACH_TIME_REMAIN",
        "MDC_DIA_THERAPY_TIME_REMAIN",
    ],
} as const;
