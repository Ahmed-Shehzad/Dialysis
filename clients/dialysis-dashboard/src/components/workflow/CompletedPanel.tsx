import type { TreatmentSessionContext } from "../../types";
import { getObsValue, MDC } from "../../utils/observations";

interface CompletedPanelProps {
    session: TreatmentSessionContext;
    onSign: () => void;
    isSigning?: boolean;
    signError?: string | null;
    onSessionChange?: (sessionId: string | null) => void;
}

export function CompletedPanel({
    session,
    onSign,
    isSigning = false,
    signError = null,
}: Readonly<CompletedPanelProps>) {
    const obs = session.observations ?? [];
    const postWeight = getObsValue(obs, MDC.WGT_POSTDIAL);
    const preWeight = getObsValue(obs, MDC.WGT_PREDIAL);
    const bpSys = getObsValue(obs, MDC.BP_SYS);
    const bpDia = getObsValue(obs, MDC.BP_DIA);
    const ufActual = getObsValue(obs, MDC.UF_ACTUAL);
    const ufTarget = getObsValue(obs, MDC.UF_TARGET);

    const ufVariance =
        ufActual != null && ufTarget != null
            ? Number(ufActual) - Number(ufTarget)
            : null;

    return (
        <div className="rounded-lg border border-blue-200 bg-blue-50/30 p-6">
            <div className="mb-4 flex items-center gap-2">
                <span className="rounded bg-blue-600 px-2 py-0.5 text-xs font-medium text-white">
                    Completed
                </span>
            </div>
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
                <Field label="Post-weight" value={postWeight ?? "—"} unit="kg" />
                <Field label="Pre-weight" value={preWeight ?? "—"} unit="kg" />
                <Field label="Final BP" value={bpSys && bpDia ? `${bpSys}/${bpDia}` : "—"} unit="mmHg" />
                <Field label="UF achieved" value={ufActual ?? "—"} unit="mL" />
                <Field label="UF target" value={ufTarget ?? "—"} unit="mL" />
                <Field
                    label="UF variance"
                    value={ufVariance != null ? `${ufVariance >= 0 ? "+" : ""}${ufVariance}` : "—"}
                    unit="mL"
                />
            </div>
            <div className="mt-6 space-y-4">
                <div>
                    <span className="text-xs font-medium text-blue-800">Notes</span>
                    <p className="mt-0.5 text-sm text-gray-600">—</p>
                </div>
                <div>
                    <span className="text-xs font-medium text-blue-800">Adequacy metrics</span>
                    <p className="mt-0.5 text-sm text-gray-600">(If calculated — placeholder)</p>
                </div>
            </div>
            <div className="mt-6 space-y-2">
                {signError && (
                    <p className="text-sm text-red-600">{signError}</p>
                )}
                <button
                    type="button"
                    onClick={onSign}
                    disabled={isSigning}
                    className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    {isSigning ? "Signing…" : "Sign Session"}
                </button>
            </div>
        </div>
    );
}

function Field({
    label,
    value,
    unit,
}: {
    label: string;
    value: string;
    unit?: string;
}) {
    return (
        <div>
            <span className="text-xs font-medium text-blue-800">{label}</span>
            <p className="mt-0.5 text-sm">
                {value}
                {unit && <span className="text-blue-600"> {unit}</span>}
            </p>
        </div>
    );
}
