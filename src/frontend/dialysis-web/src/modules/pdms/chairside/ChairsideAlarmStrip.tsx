import { humanizeError } from "@/lib/api/humanizeError";
import {
  machineLabel,
  useAcknowledgeAlarm,
  useActiveAlarms,
  type AlarmListItem,
} from "./alarmsApi";

const STATE_TONE: Record<AlarmListItem["state"], string> = {
  present: "border-rose-500 bg-rose-950/40 text-rose-100",
  inactivating: "border-amber-500 bg-amber-950/30 text-amber-100",
  resolved: "border-slate-700 bg-slate-900/40 text-slate-300",
};

const STATE_LABEL: Record<AlarmListItem["state"], string> = {
  present: "Active",
  inactivating: "Clearing",
  resolved: "Resolved",
};

/**
 * Chairside alarm strip. Renders a high-visibility "all clear" indicator when no alarms
 * are active, or a pill per alarm otherwise. Sized to read at a glance from across the
 * room — colour escalates with severity and an unacknowledged active alarm shows a slow
 * pulse so the eye is drawn even without staring at the strip.
 *
 * The Acknowledge action records the authenticated clinician as the first responder.
 * Acknowledgement is idempotent on the server (first ack wins); the SPA stamps an
 * optimistic "you" so the pulse stops the moment the button is clicked.
 */
export const ChairsideAlarmStrip = () => {
  const { data: alarms } = useActiveAlarms();
  const ack = useAcknowledgeAlarm();

  if (alarms.length === 0) {
    return (
      <section
        role="status"
        aria-label="Active alarms"
        className="flex items-center gap-3 rounded-lg border border-emerald-700/40 bg-emerald-950/30 px-4 py-2 text-sm text-emerald-200"
      >
        <span aria-hidden className="inline-block h-2.5 w-2.5 rounded-full bg-emerald-400" />
        No active alarms
      </section>
    );
  }

  return (
    <section
      role="alert"
      aria-label="Active alarms"
      className="space-y-2 rounded-lg border border-rose-700 bg-rose-950/40 p-3"
    >
      <p className="text-xs font-semibold uppercase tracking-wide text-rose-200">
        Active alarms — verify before any action
      </p>
      <ul className="flex flex-wrap gap-2">
        {alarms.map((a) => (
          <li
            key={a.id}
            title={`Code ${a.alarmCode}${a.alarmPhase ? ` · ${a.alarmPhase}` : ""}`}
            className={`flex items-center gap-2 rounded-md border-2 px-3 py-1.5 text-sm ${STATE_TONE[a.state]} ${
              a.state === "present" && !a.acknowledgedUtc ? "animate-pulse" : ""
            }`}
          >
            <span className="font-medium">{machineLabel(a)}</span>
            <span className="text-xs uppercase tracking-wide opacity-80">
              {a.alarmSource ?? `Code ${a.alarmCode}`}
            </span>
            <span className="rounded-full bg-black/30 px-2 py-0.5 text-xs">
              {STATE_LABEL[a.state]}
            </span>
            {a.acknowledgedUtc ? (
              <span
                className="rounded-full bg-emerald-900/60 px-2 py-0.5 text-xs text-emerald-200"
                title={`Acknowledged by ${a.acknowledgedBy ?? "—"}`}
              >
                Ack ✓
              </span>
            ) : (
              <button
                type="button"
                onClick={() => ack.mutate(a.id)}
                disabled={ack.isPending}
                className="rounded-md border border-rose-300/60 bg-rose-900/30 px-2 py-0.5 text-xs font-medium text-rose-100 transition hover:bg-rose-900/70 disabled:opacity-50"
              >
                Acknowledge
              </button>
            )}
          </li>
        ))}
      </ul>
      {ack.error && (
        <p role="alert" className="text-xs text-rose-300">
          {humanizeError(ack.error)}
        </p>
      )}
    </section>
  );
};
