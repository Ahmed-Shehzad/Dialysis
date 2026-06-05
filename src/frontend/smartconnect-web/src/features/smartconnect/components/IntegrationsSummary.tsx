import { useQuery } from "@tanstack/react-query";
import { fetchFlows } from "../api/flows";
import {
  FlowRuntimeState,
  FlowRuntimeStateLabel,
  type FlowRuntimeStateValue,
  type IntegrationFlow,
} from "../api/types";

const STATE_ORDER: FlowRuntimeStateValue[] = [
  FlowRuntimeState.Started,
  FlowRuntimeState.Paused,
  FlowRuntimeState.Stopped,
];

const TILE_TONE: Record<FlowRuntimeStateValue, string> = {
  [FlowRuntimeState.Started]: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100",
  [FlowRuntimeState.Paused]: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  [FlowRuntimeState.Stopped]: "border-slate-700 bg-slate-900/40 text-slate-200",
};

const NUMBER_TONE: Record<FlowRuntimeStateValue, string> = {
  [FlowRuntimeState.Started]: "text-emerald-50",
  [FlowRuntimeState.Paused]: "text-amber-50",
  [FlowRuntimeState.Stopped]: "text-slate-50",
};

const TAG_TOP_N = 5;

const countByState = (flows: readonly IntegrationFlow[]): Record<FlowRuntimeStateValue, number> => {
  const out: Record<FlowRuntimeStateValue, number> = {
    [FlowRuntimeState.Started]: 0,
    [FlowRuntimeState.Paused]: 0,
    [FlowRuntimeState.Stopped]: 0,
  };
  for (const f of flows) out[f.runtimeState]++;
  return out;
};

const topTags = (
  flows: readonly IntegrationFlow[],
): ReadonlyArray<{ tag: string; count: number }> => {
  const counts = new Map<string, number>();
  for (const f of flows) {
    for (const t of f.tags ?? []) {
      counts.set(t, (counts.get(t) ?? 0) + 1);
    }
  }
  return [...counts.entries()]
    .map(([tag, count]) => ({ tag, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, TAG_TOP_N);
};

/**
 * Operator at-a-glance header above the SmartConnect tab strip. Aggregates flow runtime
 * state into three colour-coded tiles (Started / Paused / Stopped) and surfaces the most
 * common vendor / category tags as chips so an operator can see the integration estate
 * before drilling into a specific tab.
 *
 * Loads from the same `/admin/flows` endpoint the Flows tab uses; TanStack Query shares
 * the cache so navigating between the summary and the tab is instantaneous.
 */
export const IntegrationsSummary = () => {
  const flows = useQuery({
    queryKey: ["smartconnect", "flows"],
    queryFn: fetchFlows,
    refetchInterval: 30_000,
    staleTime: 15_000,
  });

  if (flows.isLoading) {
    return <div className="text-sm text-slate-400">Loading integrations…</div>;
  }
  if (flows.error || !flows.data) {
    // Soft failure — let the tabs render even when the summary call fails. The Flows tab
    // will surface a clearer error if it hits the same endpoint.
    return null;
  }

  const data = flows.data;
  const counts = countByState(data);
  const tags = topTags(data);

  return (
    <section
      aria-label="Integrations summary"
      className="rounded-xl border border-slate-800 bg-slate-900/60 p-4"
    >
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-slate-400">Estate</p>
          <p className="text-2xl font-semibold text-clinic-50">
            {data.length}{" "}
            <span className="text-sm font-normal text-slate-400">
              {data.length === 1 ? "integration flow" : "integration flows"}
            </span>
          </p>
        </div>
        <div className="grid grid-cols-3 gap-3">
          {STATE_ORDER.map((state) => (
            <div
              key={state}
              className={`min-w-[6.5rem] rounded-lg border px-3 py-2 ${TILE_TONE[state]}`}
            >
              <p className="text-xs uppercase tracking-wide opacity-80">
                {FlowRuntimeStateLabel[state]}
              </p>
              <p
                className={`mt-0.5 font-mono text-3xl font-semibold tabular-nums ${NUMBER_TONE[state]}`}
              >
                {counts[state]}
              </p>
            </div>
          ))}
        </div>
      </div>

      {tags.length > 0 && (
        <div className="mt-4 flex flex-wrap items-center gap-2 border-t border-slate-800 pt-3 text-xs">
          <span className="uppercase tracking-wide text-slate-400">Vendors / tags</span>
          {tags.map((t) => (
            <span
              key={t.tag}
              className="rounded-full border border-slate-700 bg-slate-800/60 px-2 py-0.5 text-slate-200"
            >
              {t.tag}
              <span className="ml-1 text-slate-400">{t.count}</span>
            </span>
          ))}
        </div>
      )}
    </section>
  );
};
