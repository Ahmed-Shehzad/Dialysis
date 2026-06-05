import { humanizeError } from "@/lib/api/humanizeError";
import { usePatientName } from "@/features/patients/usePatientName";
import { useChairAssignments, type ChairAssignment } from "./chairBoardApi";

const minutesSince = (iso: string): number => {
  const elapsedMs = Date.now() - new Date(iso).getTime();
  return Math.max(0, Math.floor(elapsedMs / 60_000));
};

const formatElapsed = (iso: string): string => {
  const minutes = minutesSince(iso);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const remaining = minutes % 60;
  return `${hours}h ${remaining}m`;
};

const ChairTile = ({ assignment }: { assignment: ChairAssignment }) => {
  const elapsed = minutesSince(assignment.placedAtUtc);
  const tone =
    elapsed > 240
      ? "border-rose-700/70 bg-rose-950/40 text-rose-100"
      : elapsed > 60
        ? "border-amber-700/70 bg-amber-950/30 text-amber-100"
        : "border-emerald-700/70 bg-emerald-950/40 text-emerald-100";

  const { name, isLoading: nameLoading } = usePatientName(assignment.patientId);

  return (
    <article className={`rounded-xl border p-4 ${tone}`}>
      <header className="flex items-baseline justify-between gap-2">
        <h3 className="text-lg font-semibold tracking-wide">{assignment.chair}</h3>
        <span className="rounded-full bg-black/30 px-2 py-0.5 font-mono text-xs">
          {formatElapsed(assignment.placedAtUtc)}
        </span>
      </header>
      <dl className="mt-2 space-y-1 text-xs">
        <div>
          <dt className="opacity-70">Patient</dt>
          <dd className="truncate" title={`${name ?? "—"} · ${assignment.patientId}`}>
            {nameLoading ? (
              <span className="opacity-60">Resolving…</span>
            ) : (
              <>
                {name ?? <span className="opacity-60">unknown patient</span>}
                <span className="ml-1 font-mono opacity-60">
                  ({assignment.patientId.slice(0, 8)})
                </span>
              </>
            )}
          </dd>
        </div>
        <div>
          <dt className="opacity-70">Placed</dt>
          <dd>{new Date(assignment.placedAtUtc).toLocaleTimeString()}</dd>
        </div>
      </dl>
    </article>
  );
};

/**
 * Operator floor view — one tile per currently-occupied chair, fed by the in-memory
 * `ChairOccupancyProjection` PDMS maintains from HIS `PatientPlacedInChair` events.
 * The projection is in-memory; restarts blank the board until the next placement
 * event re-hydrates it. Polling at 10s gives a deterministic refresh; SignalR is
 * the natural follow-up.
 *
 * Tile colour escalates with elapsed time on the chair: ≤1h green, ≤4h amber, >4h
 * rose — a rough indicator of "how long has this patient been here", useful for an
 * at-a-glance scan of the floor.
 */
export const ChairBoardPage = () => {
  const { data, isLoading, error } = useChairAssignments();
  const assignments = data ?? [];

  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">Chair board</h2>
        <p className="text-sm text-slate-400">
          Live floor view. Each chair turns green &lt; 1 h, amber &lt; 4 h, rose &gt; 4 h.
        </p>
      </header>

      {isLoading && <div className="text-sm text-slate-400">Loading chairs…</div>}

      {error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-sm text-rose-100"
        >
          {humanizeError(error)}
        </div>
      )}

      {!isLoading && !error && assignments.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-4 text-sm text-slate-400">
          No chairs occupied. New HIS chair placements will appear here.
        </div>
      )}

      {assignments.length > 0 && (
        <section
          aria-label="Occupied chairs"
          className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
        >
          {assignments.map((a) => (
            <ChairTile key={a.chair} assignment={a} />
          ))}
        </section>
      )}
    </div>
  );
};
