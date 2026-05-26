import type { MessageLedgerEntry } from "../api/types";

type Props = {
  /** Ledger entries belonging to a single inbound message. */
  entries: MessageLedgerEntry[];
};

/**
 * Gantt-style visualisation of one inbound message's outbound dispatches. Each registered
 * outbound route gets a row; the bar spans from the earliest ledger entry to the route's own
 * `createdAtUtc`. When `OutboundRoutesSequential = false` (the new default), bars are tightly
 * clustered. When sequential, they cascade. Operators use this to confirm the parallel
 * outbound guarantee live on the wire instead of relying on the integration test alone.
 */
export const OutboundConcurrencyTimeline = ({ entries }: Props) => {
  const outbound = entries.filter(
    (e) => e.outboundRouteOrdinal !== null && e.outboundRouteOrdinal !== undefined,
  );
  if (outbound.length === 0) {
    return (
      <div className="text-xs text-slate-500">
        No outbound ledger rows for this inbound message yet.
      </div>
    );
  }

  const timestamps = outbound.map((e) => Date.parse(e.createdAtUtc));
  const earliest = Math.min(...timestamps);
  const latest = Math.max(...timestamps);
  const spread = Math.max(1, latest - earliest);
  const totalMs = Math.max(spread, 50);

  const ordered = [...outbound].sort(
    (a, b) => (a.outboundRouteOrdinal ?? 0) - (b.outboundRouteOrdinal ?? 0),
  );

  const spreadLabel = `${spread} ms across ${outbound.length} route${outbound.length === 1 ? "" : "s"}`;
  const verdict =
    spread < 100
      ? { label: "parallel", className: "text-emerald-300" }
      : spread < 500
        ? { label: "tight", className: "text-amber-300" }
        : { label: "sequential", className: "text-rose-300" };

  return (
    <div className="space-y-2">
      <div className="flex items-baseline justify-between">
        <h4 className="text-xs font-semibold text-slate-300">Outbound concurrency</h4>
        <span className={`text-xs ${verdict.className}`}>
          {spreadLabel} ({verdict.label})
        </span>
      </div>
      <div className="space-y-1">
        {ordered.map((e) => {
          const offsetMs = Date.parse(e.createdAtUtc) - earliest;
          const offsetPct = (offsetMs / totalMs) * 100;
          const widthPct = Math.max(2, Math.min(100 - offsetPct, 6));
          const ok = e.status === 2; // OutboundSent
          const bg = ok ? "bg-emerald-500/70" : "bg-rose-500/70";
          return (
            <div key={e.id} className="flex items-center gap-2">
              <div className="w-16 shrink-0 text-right text-[10px] text-slate-400">
                route {e.outboundRouteOrdinal}
              </div>
              <div className="relative h-3 grow rounded bg-slate-800/60">
                <div
                  className={`absolute top-0 h-3 rounded ${bg}`}
                  style={{ left: `${offsetPct}%`, width: `${widthPct}%` }}
                  title={`+${offsetMs} ms — ${ok ? "OutboundSent" : "OutboundFailed"}`}
                />
              </div>
              <div className="w-16 shrink-0 text-[10px] text-slate-400">+{offsetMs} ms</div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
