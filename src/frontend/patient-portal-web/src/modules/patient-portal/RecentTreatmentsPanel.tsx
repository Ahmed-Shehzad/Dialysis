import { useQuery } from "@tanstack/react-query";
import {
  fetchSessionsByPatient,
  type DialysisSessionSummary,
} from "@/features/sessions/api/sessionsApi";
import { humanizeError } from "@/lib/api/humanizeError";

const STATUS_TONE: Record<DialysisSessionSummary["status"], string> = {
  Scheduled: "border-slate-700 bg-slate-900/40 text-slate-200",
  InProgress: "border-clinic-700/70 bg-clinic-950/40 text-clinic-100",
  Paused: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  Completed: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100",
  Aborted: "border-rose-700/70 bg-rose-950/40 text-rose-100",
  Cancelled: "border-slate-700 bg-slate-900/40 text-slate-300",
};

const STATUS_LABEL: Record<DialysisSessionSummary["status"], string> = {
  Scheduled: "Scheduled",
  InProgress: "In progress",
  Paused: "Paused",
  Completed: "Completed",
  Aborted: "Aborted",
  Cancelled: "Cancelled",
};

const formatDate = (iso: string): string => new Date(iso).toLocaleDateString();
const formatTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "—";

const durationLabel = (start?: string | null, end?: string | null): string => {
  if (!start || !end) return "—";
  const minutes = Math.max(
    0,
    Math.floor((new Date(end).getTime() - new Date(start).getTime()) / 60_000),
  );
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const rem = minutes % 60;
  return `${hours}h ${rem}m`;
};

/**
 * Patient-side view of recent dialysis sessions. Reads from PDMS's new
 * `GET /api/v1.0/sessions/by-patient/{id}` endpoint with a 90-day lookback, ordered
 * most-recent first. Read-only — the patient can see "what happened, when, and how long
 * it took" without leaving the portal.
 *
 * Date / start / end / duration / status are all the patient cares about. Vitals,
 * prescription, and adverse events stay clinician-side via the chairside view.
 */
export const RecentTreatmentsPanel = ({ patientId }: { patientId: string }) => {
  const sessions = useQuery({
    queryKey: ["patient-portal", "treatments", patientId],
    queryFn: () => fetchSessionsByPatient(patientId),
    staleTime: 60_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">Recent treatments</h3>
        <p className="text-xs text-slate-400">
          Last 90 days of dialysis sessions, most recent first.
        </p>
      </header>

      {sessions.isLoading && <div className="text-xs text-slate-400">Loading your treatments…</div>}

      {sessions.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(sessions.error)}
        </div>
      )}

      {sessions.data && sessions.data.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No treatments on file in the last 90 days.
        </div>
      )}

      {sessions.data && sessions.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {sessions.data.map((s) => (
            <li key={s.id} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-3 text-slate-200">{formatDate(s.scheduledStartUtc)}</span>
              <span className="col-span-2 text-xs text-slate-400">
                {formatTime(s.actualStartUtc ?? s.scheduledStartUtc)}
              </span>
              <span className="col-span-2 text-xs text-slate-400">
                {durationLabel(s.actualStartUtc, s.actualEndUtc)}
              </span>
              <span className="col-span-2 text-xs text-slate-500" title={s.machineId ?? ""}>
                {s.machineId ? `Machine ${s.machineId.slice(0, 8)}` : "—"}
              </span>
              <span className="col-span-3 text-right">
                <span
                  className={`rounded-full border px-2 py-0.5 text-xs ${STATUS_TONE[s.status]}`}
                >
                  {STATUS_LABEL[s.status]}
                </span>
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
