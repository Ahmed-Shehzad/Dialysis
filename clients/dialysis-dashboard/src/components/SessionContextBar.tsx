import { useQuery } from "@tanstack/react-query";
import {
    getAlarmsBySession,
    getPatientByMrn,
    getTreatmentSession,
} from "../api";
import type {
    AlarmContext,
    ObservationDto,
    PatientContext,
    TreatmentSessionContext,
} from "../types";

/** MDC codes for UF and BP (IEEE 11073). */
const MDC_UF_TARGET_CODES = [
    "MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE",
    "MDC_HDIALY_UF_TARGET_VOL",
    "MDC_DIA_UF_VOL_TARGET",
] as const;
const MDC_UF_ACTUAL_CODES = [
    "MDC_HDIALY_UF_ACTUAL_REMOVED_VOL",
    "MDC_DIA_UF_VOL_TOTAL",
] as const;
const MDC_BP_SYS_CODES = ["MDC_PRESS_BLD_SYS"] as const;
const MDC_BP_DIA_CODES = ["MDC_PRESS_BLD_DIA"] as const;

function extractMdcCode(code: string | undefined): string {
    if (!code) return "";
    const part = code.split("^")[1];
    return part ?? code;
}

function findObs(
    observations: ObservationDto[],
    codeList: readonly string[]
): ObservationDto | undefined {
    return observations.find((o) => {
        const c = extractMdcCode(o.code);
        return codeList.includes(c);
    });
}

function formatAge(dob: string | undefined): string {
    if (!dob) return "—";
    const birth = new Date(dob);
    const now = new Date();
    const years = Math.floor(
        (now.getTime() - birth.getTime()) / (365.25 * 24 * 60 * 60 * 1000)
    );
    return years >= 0 ? `${years}y` : "—";
}

function formatElapsed(startedAt: string | undefined, endedAt?: string): string {
    if (!startedAt) return "—";
    const start = new Date(startedAt).getTime();
    const end = endedAt ? new Date(endedAt).getTime() : Date.now();
    const ms = Math.max(0, end - start);
    const m = Math.floor(ms / 60000);
    const s = Math.floor((ms % 60000) / 1000);
    return `${m}:${s.toString().padStart(2, "0")}`;
}

function formatDuration(min: number | undefined): string {
    if (min == null || min <= 0) return "—";
    return `${min} min`;
}

interface SessionContextBarProps {
    sessionId: string | null;
    refetchIntervalMs?: number;
}

export function SessionContextBar({
    sessionId,
    refetchIntervalMs = 10_000,
}: Readonly<SessionContextBarProps>) {
    const { data: session } = useQuery({
        queryKey: ["treatment-session", sessionId],
        queryFn: () => getTreatmentSession(sessionId!),
        enabled: Boolean(sessionId),
        refetchInterval: refetchIntervalMs,
        staleTime: 5_000,
    });

    const { data: patient } = useQuery({
        queryKey: ["patient", session?.patientMrn],
        queryFn: () => getPatientByMrn(session!.patientMrn!),
        enabled: Boolean(session?.patientMrn),
        refetchInterval: refetchIntervalMs,
        staleTime: 30_000,
    });

    const { data: alarms = [] } = useQuery({
        queryKey: ["alarms", sessionId],
        queryFn: () => getAlarmsBySession(sessionId!),
        enabled: Boolean(sessionId),
        refetchInterval: refetchIntervalMs,
        staleTime: 5_000,
    });

    if (!sessionId) {
        return (
            <div className="bg-slate-800 text-slate-200 px-4 py-3 text-sm">
                <span className="text-slate-400">
                    Select a session to view context
                </span>
            </div>
        );
    }

    return (
        <SessionContextBarInner
            session={session ?? null}
            patient={patient ?? null}
            alarms={alarms}
        />
    );
}

interface InnerProps {
    session: TreatmentSessionContext | null;
    patient: PatientContext | null;
    alarms: AlarmContext[];
}

function SessionContextBarInner({
    session,
    patient,
    alarms,
}: Readonly<InnerProps>) {
    const isLoading = !session;
    const observations = session?.observations ?? [];
    const ufTarget = findObs(observations, MDC_UF_TARGET_CODES);
    const ufActual = findObs(observations, MDC_UF_ACTUAL_CODES);
    const bpSys = findObs(observations, MDC_BP_SYS_CODES);
    const bpDia = findObs(observations, MDC_BP_DIA_CODES);
    const lastBp =
        bpSys || bpDia
            ? [bpSys?.value, bpDia?.value].filter(Boolean).join("/")
            : null;

    const activeAlarms = alarms.filter(
        (a) =>
            a.alarmState?.toLowerCase() === "active" ||
            a.alarmState?.toLowerCase() === "latched" ||
            a.alarmState?.toLowerCase() === "acknowledged"
    );

    if (isLoading) {
        return (
            <div className="bg-slate-800 text-slate-200 px-4 py-3 text-sm animate-pulse">
                Loading session context…
            </div>
        );
    }

    const patientName = patient
        ? `${patient.firstName} ${patient.lastName}`.trim() || "—"
        : "—";
    const mrn = patient?.medicalRecordNumber ?? session?.patientMrn ?? "—";
    const dob = patient?.dateOfBirth;
    const age = formatAge(dob);
    const elapsed = formatElapsed(session?.startedAt, session?.endedAt);
    const prescribed = session?.therapyTimePrescribedMin;
    const status = session?.status ?? "Unknown";

    const statusBadgeClass =
        status.toLowerCase() === "active"
            ? "bg-emerald-600"
            : status.toLowerCase() === "completed"
              ? "bg-slate-500"
              : "bg-amber-600";

    return (
        <div className="bg-slate-800 text-slate-200 px-4 py-3 text-sm border-b border-slate-700">
            <div className="flex flex-wrap gap-x-6 gap-y-2">
                {/* Patient Context */}
                <section className="flex flex-wrap gap-x-4 gap-y-1">
                    <span className="text-slate-400 font-medium">
                        Patient
                    </span>
                    <span>{patientName}</span>
                    <span className="text-slate-400">MRN</span>
                    <span>{mrn}</span>
                    <span className="text-slate-400">DOB</span>
                    <span>{dob ?? "—"}</span>
                    <span className="text-slate-400">Age</span>
                    <span>{age}</span>
                    <span className="text-amber-300 font-semibold">
                        Allergies
                    </span>
                    <span className="text-amber-200">—</span>
                    <span className="text-slate-400">Access</span>
                    <span>—</span>
                    <span className="text-slate-400">Isolation</span>
                    <span>—</span>
                </section>

                <div className="h-4 w-px bg-slate-600 self-center" />

                {/* Session Context */}
                <section className="flex flex-wrap gap-x-4 gap-y-1">
                    <span className="text-slate-400 font-medium">
                        Session
                    </span>
                    <span
                        className={`px-2 py-0.5 rounded text-xs font-medium ${statusBadgeClass}`}
                    >
                        {status}
                    </span>
                    <span className="text-slate-400">Elapsed</span>
                    <span>{elapsed}</span>
                    <span className="text-slate-400">Prescribed</span>
                    <span>{formatDuration(prescribed)}</span>
                    <span className="text-slate-400">UF</span>
                    <span>
                        {ufActual?.value ?? "—"} / {ufTarget?.value ?? "—"}{" "}
                        {ufTarget?.unit ?? "mL"}
                    </span>
                    <span className="text-slate-400">Nurse</span>
                    <span>—</span>
                </section>

                <div className="h-4 w-px bg-slate-600 self-center" />

                {/* Safety Indicators */}
                <section className="flex flex-wrap gap-x-4 gap-y-1">
                    <span className="text-slate-400 font-medium">
                        Safety
                    </span>
                    <span className="text-slate-400">Alerts</span>
                    {activeAlarms.length > 0 ? (
                        <span className="text-red-400 font-semibold">
                            {activeAlarms.length} active
                        </span>
                    ) : (
                        <span className="text-emerald-400">None</span>
                    )}
                    <span className="text-slate-400">Last BP</span>
                    <span>{lastBp ?? "—"} mmHg</span>
                    <span className="text-slate-400">High-risk</span>
                    <span>—</span>
                </section>
            </div>
        </div>
    );
}
