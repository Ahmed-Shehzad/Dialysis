import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  fetchActiveSessions,
  type DialysisSessionSummary,
} from "@/features/sessions/api/sessionsApi";
import { ManagerDashboardCards } from "@/features/his/components/ManagerDashboardCards";
import { IntegrationEventsTable } from "@/features/his/components/IntegrationEventsTable";

const statusClass: Record<DialysisSessionSummary["status"], string> = {
  Scheduled: "text-sky-300",
  InProgress: "text-emerald-300",
  Paused: "text-amber-300",
  Completed: "text-slate-300",
  Aborted: "text-rose-300",
  Cancelled: "text-slate-500",
};

export const DashboardPage = () => {
  const { data, isLoading, error } = useQuery({
    queryKey: ["pdms", "sessions", "active"],
    queryFn: () => fetchActiveSessions(true),
    refetchInterval: 30_000,
  });

  return (
    <div className="space-y-8">
      <section>
        <h2 className="mb-3 text-xl font-semibold text-clinic-50">Operations snapshot</h2>
        <ManagerDashboardCards />
      </section>

      <section>
        <h2 className="mb-3 text-xl font-semibold text-clinic-50">Active dialysis sessions</h2>
        <p className="mb-3 text-sm text-slate-400">
          From the PDMS module. Open a session to watch live vitals streamed via SignalR.
        </p>

        {isLoading && <div className="text-slate-400">Loading…</div>}
        {error && (
          <div className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100">
            PDMS unavailable.
          </div>
        )}

        {data && data.length === 0 && (
          <div className="rounded-md border border-slate-700 bg-slate-900/40 p-4 text-slate-300">
            No active sessions.
          </div>
        )}

        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {data?.map((s) => (
            <li key={s.id}>
              <Link
                to={`/sessions/${s.id}`}
                className="block rounded-lg border border-slate-800 bg-slate-900/60 p-4 transition hover:border-clinic-600"
              >
                <div className="font-mono text-xs text-slate-400">{s.id.slice(0, 8)}…</div>
                <div className="mt-1 text-sm text-slate-200">Patient {s.patientId.slice(0, 8)}</div>
                <div className={`mt-2 text-xs font-medium ${statusClass[s.status]}`}>{s.status}</div>
              </Link>
            </li>
          ))}
        </ul>
      </section>

      <IntegrationEventsTable />
    </div>
  );
};
