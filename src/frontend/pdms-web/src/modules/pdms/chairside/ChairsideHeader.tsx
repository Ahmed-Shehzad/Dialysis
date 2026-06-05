import { Link } from "react-router-dom";
import type { DialysisSessionSummary } from "@/features/sessions/api/sessionsApi";
import { StatusBadge, type Status as RealtimeStatus } from "@/components/ui/StatusBadge";
import { usePatientContext } from "@/shell/PatientContextProvider";
import { useElapsedTime } from "./useElapsedTime";

const STATUS_TONE: Record<DialysisSessionSummary["status"], string> = {
  Scheduled: "text-sky-300",
  InProgress: "text-emerald-300",
  Paused: "text-amber-300",
  Completed: "text-slate-300",
  Aborted: "text-rose-300",
  Cancelled: "text-slate-500",
};

interface ChairsideHeaderProps {
  session: DialysisSessionSummary | undefined;
  sessionId: string;
  realtimeStatus: RealtimeStatus;
}

/**
 * Chairside-format header for the live treatment page. Sized so a nurse can read it
 * standing two metres from a tablet on the machine cart: large station identifier
 * (machine id stands in for chair), patient name (from the cross-module patient context
 * once HIS has selected one), session status, treatment usage time (machine on-time, ticking
 * once per second while running and frozen once the session ends), and a realtime-stream pulse.
 */
export const ChairsideHeader = ({ session, sessionId, realtimeStatus }: ChairsideHeaderProps) => {
  const { patient } = usePatientContext();
  const usageTime = useElapsedTime(session?.actualStartUtc, {
    endUtc: session?.actualEndUtc,
    pausedAtUtc: session?.pausedAtUtc,
    pausedSeconds: session?.accumulatedPausedSeconds,
  });
  const isPaused = session?.status === "Paused";
  const station = session?.machineId ?? "Live session";
  const patientLabel =
    patient && session && patient.id === session.patientId
      ? patient.displayName
      : session
        ? `Patient ${session.patientId.slice(0, 8)}…`
        : `Session ${sessionId.slice(0, 8)}…`;
  const mrn = patient && session && patient.id === session.patientId ? patient.mrn : undefined;
  const statusTone = session ? STATUS_TONE[session.status] : "text-slate-400";

  return (
    <header className="rounded-xl border border-slate-800 bg-slate-900/70 px-5 py-4">
      <div className="flex flex-wrap items-center gap-x-8 gap-y-3">
        <div className="min-w-0">
          <p className="text-xs uppercase tracking-wide text-slate-400">Station</p>
          <p className="text-3xl font-semibold text-clinic-50">{station}</p>
        </div>

        <div className="min-w-0 flex-1">
          <p className="text-xs uppercase tracking-wide text-slate-400">Patient</p>
          <p className="truncate text-2xl font-semibold text-clinic-50">
            {patientLabel}
            {session?.patientId && (
              <Link
                to={`/patients/${session.patientId}`}
                className="ml-2 align-middle text-xs font-normal text-clinic-300 hover:underline"
              >
                Open chart
              </Link>
            )}
          </p>
          {mrn && <p className="text-xs text-slate-400">{mrn}</p>}
        </div>

        <div>
          <p className="text-xs uppercase tracking-wide text-slate-400">
            Treatment time{isPaused && <span className="ml-1 text-amber-300">· paused</span>}
          </p>
          <p
            className={`font-mono text-3xl font-semibold tabular-nums ${
              isPaused ? "text-amber-300" : "text-clinic-50"
            }`}
            aria-label="Treatment usage time (dialysis machine on-time, excluding pauses)"
          >
            {usageTime}
          </p>
        </div>

        <div>
          <p className="text-xs uppercase tracking-wide text-slate-400">Status</p>
          <p className={`text-2xl font-semibold ${statusTone}`}>{session?.status ?? "—"}</p>
        </div>

        <div className="flex flex-col items-end gap-1 text-xs text-slate-300">
          <span className="uppercase tracking-wide text-slate-400">Realtime</span>
          <StatusBadge status={realtimeStatus} />
        </div>
      </div>
    </header>
  );
};
