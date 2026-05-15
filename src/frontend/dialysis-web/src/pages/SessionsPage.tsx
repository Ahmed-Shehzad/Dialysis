import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { fetchActiveSessions, type DialysisSessionSummary } from "@/features/sessions/api/sessionsApi";
import { ScheduleSessionDialog } from "@/features/sessions/components/ScheduleSessionDialog";

type StatusFilter = "all" | "active" | DialysisSessionSummary["status"];

const STATUS_OPTIONS: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "All (last 7 days)" },
  { value: "active", label: "Active only" },
  { value: "Scheduled", label: "Scheduled" },
  { value: "InProgress", label: "In progress" },
  { value: "Paused", label: "Paused" },
  { value: "Completed", label: "Completed" },
  { value: "Aborted", label: "Aborted" },
  { value: "Cancelled", label: "Cancelled" },
];

const statusBadge = (status: DialysisSessionSummary["status"]) => {
  const map: Record<DialysisSessionSummary["status"], string> = {
    Scheduled: "bg-slate-700 text-slate-200",
    InProgress: "bg-clinic-600 text-white",
    Paused: "bg-amber-600 text-white",
    Completed: "bg-emerald-700 text-white",
    Aborted: "bg-rose-700 text-white",
    Cancelled: "bg-slate-600 text-slate-200",
  };
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${map[status]}`}>
      {status}
    </span>
  );
};

const formatDateTime = (iso: string | null | undefined) =>
  iso ? new Date(iso).toLocaleString() : "—";

export const SessionsPage = () => {
  const [filter, setFilter] = useState<StatusFilter>("all");
  const [dialogOpen, setDialogOpen] = useState(false);

  const query = useQuery({
    queryKey: ["pdms", "sessions", "list", filter === "active" ? "active" : "recent"],
    queryFn: () => fetchActiveSessions(filter === "active"),
    refetchInterval: 10_000,
  });

  const filtered = useMemo(() => {
    const all = query.data ?? [];
    if (filter === "all" || filter === "active") return all;
    return all.filter((s) => s.status === filter);
  }, [query.data, filter]);

  const grouped = useMemo(() => {
    const active = filtered.filter((s) =>
      ["Scheduled", "InProgress", "Paused"].includes(s.status),
    );
    const finished = filtered.filter((s) =>
      ["Completed", "Aborted", "Cancelled"].includes(s.status),
    );
    return { active, finished };
  }, [filtered]);

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold text-clinic-50">Dialysis sessions</h2>
          <p className="text-sm text-slate-400">
            Schedule, monitor, and review hemodialysis sessions.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as StatusFilter)}
            className="rounded-md border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-100"
          >
            {STATUS_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => setDialogOpen(true)}
            className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-clinic-700"
          >
            + Schedule session
          </button>
        </div>
      </header>

      {query.isLoading && <div className="text-sm text-slate-400">Loading sessions…</div>}
      {query.error && (
        <div className="rounded-md border border-rose-800 bg-rose-950/40 p-3 text-sm text-rose-200">
          Failed to load sessions.
        </div>
      )}

      {!query.isLoading && filtered.length === 0 && (
        <div className="rounded-md border border-slate-800 bg-slate-900/40 p-6 text-center text-sm text-slate-400">
          No sessions match the current filter. Click <span className="text-clinic-300">+ Schedule session</span> to create one.
        </div>
      )}

      {grouped.active.length > 0 && (
        <SessionsTable title="Active / scheduled" rows={grouped.active} />
      )}
      {grouped.finished.length > 0 && (
        <SessionsTable title="Recently completed" rows={grouped.finished} />
      )}

      <ScheduleSessionDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
    </div>
  );
};

const SessionsTable = ({ title, rows }: { title: string; rows: DialysisSessionSummary[] }) => (
  <section className="rounded-lg border border-slate-800 bg-slate-900/40">
    <div className="border-b border-slate-800 px-4 py-2 text-xs uppercase tracking-wide text-slate-400">
      {title} · {rows.length}
    </div>
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="bg-slate-900/60 text-left text-xs uppercase text-slate-400">
          <tr>
            <th className="px-4 py-2">Session</th>
            <th className="px-4 py-2">Patient</th>
            <th className="px-4 py-2">Status</th>
            <th className="px-4 py-2">Scheduled</th>
            <th className="px-4 py-2">Started</th>
            <th className="px-4 py-2">Ended</th>
            <th className="px-4 py-2">Machine</th>
            <th className="px-4 py-2" />
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-800">
          {rows.map((s) => (
            <tr key={s.id} className="hover:bg-slate-900/60">
              <td className="px-4 py-2 font-mono text-xs text-slate-300">{s.id.slice(0, 8)}</td>
              <td className="px-4 py-2 font-mono text-xs text-slate-300">{s.patientId.slice(0, 8)}</td>
              <td className="px-4 py-2">{statusBadge(s.status)}</td>
              <td className="px-4 py-2 text-xs text-slate-400">{formatDateTime(s.scheduledStartUtc)}</td>
              <td className="px-4 py-2 text-xs text-slate-400">{formatDateTime(s.actualStartUtc)}</td>
              <td className="px-4 py-2 text-xs text-slate-400">{formatDateTime(s.actualEndUtc)}</td>
              <td className="px-4 py-2 font-mono text-xs text-slate-400">
                {s.machineId ? s.machineId.slice(0, 8) : "—"}
              </td>
              <td className="px-4 py-2 text-right">
                <Link
                  to={`/sessions/${s.id}`}
                  className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800"
                >
                  Open →
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  </section>
);
