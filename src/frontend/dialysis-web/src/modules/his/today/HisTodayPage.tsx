import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { humanizeError } from "@/lib/api/humanizeError";
import { AssignChairDialog } from "./AssignChairDialog";
import { CheckInDialog } from "./CheckInDialog";
import { QueueCard } from "./QueueCard";
import { WalkInDialog } from "./WalkInDialog";
import { useTodaysQueue, type QueueEntry, type QueueStatus } from "./queueApi";

const COLUMNS: ReadonlyArray<{ status: QueueStatus; title: string; emptyHint: string }> = [
  {
    status: "expected",
    title: "Expected today",
    emptyHint: "No appointments left to greet — the day's check-ins are done.",
  },
  {
    status: "waiting",
    title: "Waiting",
    emptyHint: "Nobody is waiting for a chair right now.",
  },
  {
    status: "in-treatment",
    title: "In treatment",
    emptyHint: "No active treatments. They'll appear here once a chair is assigned.",
  },
];

const todayLabel = (): string =>
  new Date().toLocaleDateString([], {
    weekday: "long",
    day: "numeric",
    month: "long",
  });

export const HisTodayPage = () => {
  const navigate = useNavigate();
  const queue = useTodaysQueue();
  const [checkInTarget, setCheckInTarget] = useState<QueueEntry | null>(null);
  const [assignChairTarget, setAssignChairTarget] = useState<QueueEntry | null>(null);
  const [walkInOpen, setWalkInOpen] = useState(false);

  const grouped = useMemo(() => {
    const groups: Record<QueueStatus, QueueEntry[]> = {
      expected: [],
      waiting: [],
      "in-treatment": [],
    };
    for (const entry of queue.data ?? []) {
      groups[entry.status].push(entry);
    }
    return groups;
  }, [queue.data]);

  const handleAction = (entry: QueueEntry) => {
    // Each column's primary action is a forward step in the queue.
    // Expected → check-in dialog. Waiting → assign-chair dialog.
    // In treatment → cross into the EHR chart.
    if (entry.status === "expected") {
      setCheckInTarget(entry);
      return;
    }
    if (entry.status === "waiting") {
      setAssignChairTarget(entry);
      return;
    }
    navigate(`/patients/${entry.patientId}`);
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-slate-400">Front Desk</p>
          <h2 className="text-2xl font-semibold text-clinic-50">{todayLabel()}</h2>
        </div>
        <button
          type="button"
          onClick={() => setWalkInOpen(true)}
          className="rounded-md bg-clinic-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-clinic-500"
        >
          + Walk-in
        </button>
      </header>

      {queue.isLoading && <div className="text-slate-400">Loading today's queue…</div>}

      {queue.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100"
        >
          {humanizeError(queue.error)}
        </div>
      )}

      {checkInTarget && (
        <CheckInDialog entry={checkInTarget} onClose={() => setCheckInTarget(null)} />
      )}

      {assignChairTarget && (
        <AssignChairDialog entry={assignChairTarget} onClose={() => setAssignChairTarget(null)} />
      )}

      {walkInOpen && <WalkInDialog onClose={() => setWalkInOpen(false)} />}

      {queue.data && (
        <div className="grid gap-4 md:grid-cols-3">
          {COLUMNS.map((col) => {
            const items = grouped[col.status];
            return (
              <section
                key={col.status}
                aria-labelledby={`col-${col.status}`}
                className="flex flex-col gap-3"
              >
                <h3
                  id={`col-${col.status}`}
                  className="flex items-baseline justify-between text-sm font-medium text-slate-300"
                >
                  <span>{col.title}</span>
                  <span className="text-xs text-slate-500">{items.length}</span>
                </h3>
                {items.length === 0 ? (
                  <p className="rounded-md border border-dashed border-slate-700 p-4 text-xs text-slate-500">
                    {col.emptyHint}
                  </p>
                ) : (
                  <ul className="flex flex-col gap-3">
                    {items.map((entry) => (
                      <li key={entry.id}>
                        <QueueCard entry={entry} onAction={handleAction} />
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
};
