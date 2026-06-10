import { usePatientContext } from "@/shell/PatientContextProvider";
import type { QueueEntry, QueueStatus } from "./queueApi";

const STATUS_STYLES: Record<QueueStatus, string> = {
  expected: "bg-sky-900/40 text-sky-200 border-sky-700",
  waiting: "bg-amber-900/40 text-amber-200 border-amber-700",
  "in-treatment": "bg-emerald-900/40 text-emerald-200 border-emerald-700",
};

const ACTION_LABEL: Record<QueueStatus, string> = {
  expected: "Check in",
  waiting: "Assign chair",
  "in-treatment": "Open chart",
};

const STATUS_LABEL: Record<QueueStatus, string> = {
  expected: "Expected",
  waiting: "Waiting",
  "in-treatment": "In treatment",
};

const formatTime = (iso: string): string => {
  try {
    return new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  } catch {
    return iso;
  }
};

const initialsOf = (name: string): string =>
  name
    .split(/\s+/u)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");

interface QueueCardProps {
  entry: QueueEntry;
  onAction(entry: QueueEntry): void;
}

/**
 * One patient in the receptionist's queue. The whole card is the affordance to pull this
 * patient into the cross-module patient context; the action button performs the
 * status-appropriate next step (check in / assign chair / open chart).
 */
export const QueueCard = ({ entry, onAction }: QueueCardProps) => {
  const { select } = usePatientContext();
  const selectThisPatient = () =>
    select({ id: entry.patientId, displayName: entry.patientName, mrn: entry.mrn });

  return (
    <article className="flex flex-col gap-3 rounded-lg border border-slate-800 bg-slate-900/60 p-4 transition hover:border-clinic-600">
      <header className="flex items-start gap-3">
        <span
          aria-hidden
          className="flex h-10 w-10 flex-none items-center justify-center rounded-full bg-clinic-700 text-sm font-semibold text-clinic-50"
        >
          {initialsOf(entry.patientName)}
        </span>
        <div className="min-w-0 flex-1">
          <button
            type="button"
            onClick={selectThisPatient}
            className="block truncate text-left text-base font-medium text-slate-100 hover:text-clinic-200"
          >
            {entry.patientName}
          </button>
          <p className="truncate text-xs text-slate-400">
            {entry.mrn} · {formatTime(entry.scheduledForUtc)}
            {entry.chair ? ` · ${entry.chair}` : ""}
          </p>
        </div>
        <span
          className={`flex-none rounded-full border px-2 py-0.5 text-xs ${STATUS_STYLES[entry.status]}`}
        >
          {STATUS_LABEL[entry.status]}
        </span>
      </header>

      <div className="flex items-center justify-between text-xs">
        <span className={entry.eligibilityVerified ? "text-emerald-300" : "text-amber-300"}>
          {entry.eligibilityVerified ? "Insurance verified" : "Verify insurance"}
        </span>
        <button
          type="button"
          onClick={() => onAction(entry)}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-xs font-medium text-white transition hover:bg-clinic-500"
        >
          {ACTION_LABEL[entry.status]}
        </button>
      </div>
    </article>
  );
};
