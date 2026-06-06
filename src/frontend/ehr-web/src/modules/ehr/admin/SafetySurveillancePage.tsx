import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchSurveillance } from "@/features/safety/api/safetyApi";
import { humanizeError } from "@/lib/api/humanizeError";

const WINDOWS = [7, 14, 30];

/**
 * Cross-patient patient-safety surveillance — adverse-event counts by kind/severity over a window, with
 * spike flags (a kind materially up vs the prior baseline). The pattern a single chart can't show.
 */
export const SafetySurveillancePage = () => {
  const [windowDays, setWindowDays] = useState(7);

  const surveillance = useQuery({
    queryKey: ["ehr", "safety", "surveillance", windowDays],
    queryFn: () => fetchSurveillance(windowDays),
  });

  const data = surveillance.data;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-slate-400">Patient safety</p>
          <h2 className="text-2xl font-semibold text-clinic-50">Adverse-event surveillance</h2>
          <p className="text-xs text-slate-400">
            Intradialytic adverse events across the panel — spot a spike before it becomes a
            pattern.
          </p>
        </div>
        <div className="flex items-center gap-1">
          {WINDOWS.map((w) => (
            <button
              key={w}
              type="button"
              onClick={() => setWindowDays(w)}
              className={`rounded-md px-2.5 py-1 text-xs transition ${
                windowDays === w
                  ? "bg-clinic-700 text-white"
                  : "border border-slate-700 text-slate-300 hover:border-slate-500"
              }`}
            >
              {w}d
            </button>
          ))}
        </div>
      </header>

      {surveillance.isLoading && <p className="text-sm text-slate-400">Loading surveillance…</p>}
      {surveillance.error && (
        <p role="alert" className="text-sm text-rose-300">
          {humanizeError(surveillance.error)}
        </p>
      )}

      {data && (
        <>
          {data.spikes.length > 0 && (
            <section className="rounded-lg border-2 border-rose-600 bg-rose-950/40 p-4">
              <h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-rose-200">
                Spikes — review
              </h3>
              <ul className="space-y-1">
                {data.spikes.map((s) => (
                  <li key={s.kind} className="text-sm text-rose-100">
                    <span className="font-mono">{s.kind}</span> — {s.currentCount} this window vs{" "}
                    {s.baselineCount} baseline
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
            <h3 className="mb-2 text-sm font-medium text-slate-200">
              Events by kind & severity{" "}
              <span className="text-slate-500">
                ({data.total} in {data.windowDays}d)
              </span>
            </h3>
            {data.buckets.length === 0 ? (
              <p className="text-xs text-slate-500">No adverse events in this window.</p>
            ) : (
              <ul className="divide-y divide-slate-800 text-sm">
                {data.buckets.map((b) => (
                  <li
                    key={`${b.kind}-${b.severity}`}
                    className="flex items-center justify-between gap-2 py-2"
                  >
                    <span>
                      <span className="font-mono text-slate-200">{b.kind}</span>
                      <span className="ml-2 text-xs uppercase tracking-wide text-slate-400">
                        {b.severity}
                      </span>
                    </span>
                    <span className="rounded-full bg-slate-800 px-2.5 py-0.5 text-xs text-slate-200">
                      {b.count}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </>
      )}
    </div>
  );
};
