import { useQuery } from "@tanstack/react-query";
import { fetchPrunerOptions } from "../api/pruner";

export const RetentionTab = () => {
  const opts = useQuery({
    queryKey: ["smartconnect", "pruner-options"],
    queryFn: fetchPrunerOptions,
  });

  return (
    <section className="space-y-4">
      <div>
        <h3 className="text-sm font-medium text-slate-200">Retention (pruner)</h3>
        <p className="text-xs text-slate-500">
          The pruner runs on a fixed schedule and removes ledger/attachment rows older than the
          retention window. v1 exposes the live configuration read-only — values are bound to the
          host's <code className="text-slate-400">SmartConnect:Pruner</code> options at startup.
        </p>
      </div>

      {opts.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
      {opts.error && <div className="text-xs text-rose-300">Pruner options unavailable.</div>}
      {opts.data && (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4">
            <div className="text-xs uppercase text-slate-500">Sweep interval</div>
            <div className="mt-1 text-2xl font-semibold text-slate-100">
              {opts.data.intervalHours.toFixed(1)}{" "}
              <span className="text-base text-slate-400">h</span>
            </div>
            <div className="mt-1 text-xs text-slate-500">
              raw: <code className="text-slate-300">{opts.data.interval}</code>
            </div>
          </div>
          <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4">
            <div className="text-xs uppercase text-slate-500">Retention window</div>
            <div className="mt-1 text-2xl font-semibold text-slate-100">
              {opts.data.retentionDays.toFixed(1)}{" "}
              <span className="text-base text-slate-400">days</span>
            </div>
            <div className="mt-1 text-xs text-slate-500">
              raw: <code className="text-slate-300">{opts.data.retentionPeriod}</code>
            </div>
          </div>
        </div>
      )}

      <p className="text-xs text-slate-500">
        To change these values, update{" "}
        <code className="text-slate-400">SmartConnect:Pruner:Interval</code> /
        <code className="text-slate-400"> :RetentionPeriod</code> in the module's configuration and
        restart. Editable retention is on the backlog.
      </p>
    </section>
  );
};
