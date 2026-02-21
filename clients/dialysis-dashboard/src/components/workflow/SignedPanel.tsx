import type { TreatmentSessionContext } from "../../types";
import { getObsValue, MDC } from "../../utils/observations";

interface SignedPanelProps {
    session: TreatmentSessionContext;
    onSessionChange?: (sessionId: string | null) => void;
}

export function SignedPanel({
    session,
}: Readonly<SignedPanelProps>) {
    const obs = session.observations ?? [];
    const postWeight = getObsValue(obs, MDC.WGT_POSTDIAL);
    const ufActual = getObsValue(obs, MDC.UF_ACTUAL);
    const ufTarget = getObsValue(obs, MDC.UF_TARGET);

    return (
        <div className="rounded-lg border border-slate-300 bg-slate-50 p-6">
            <div className="mb-4 flex items-center gap-2">
                <span className="rounded bg-slate-600 px-2 py-0.5 text-xs font-medium text-white">
                    Signed / Locked
                </span>
            </div>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <ReadOnlyField label="Session" value={session.sessionId} />
                <ReadOnlyField label="Status" value={session.status} />
                <ReadOnlyField label="Post-weight" value={postWeight ?? "—"} unit="kg" />
                <ReadOnlyField label="UF removed" value={ufActual ?? "—"} unit="mL" />
                <ReadOnlyField label="UF target" value={ufTarget ?? "—"} unit="mL" />
            </div>
            <div className="mt-6">
                <span className="text-xs font-medium text-slate-600">Audit trail</span>
                <p className="mt-0.5 text-sm text-slate-500">(Placeholder — signed by clinician)</p>
            </div>
            <div className="mt-4">
                <span className="text-xs font-medium text-slate-600">Billing status</span>
                <p className="mt-0.5 text-sm text-slate-500">—</p>
            </div>
        </div>
    );
}

function ReadOnlyField({
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
            <span className="text-xs font-medium text-slate-600">{label}</span>
            <p className="mt-0.5 text-sm text-slate-800">
                {value}
                {unit && <span className="text-slate-500"> {unit}</span>}
            </p>
        </div>
    );
}
