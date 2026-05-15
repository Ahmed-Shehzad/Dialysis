import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchAuditEvents, type AuditEventQuery } from "../api/auditEvents";
import { fetchFlows } from "../api/flows";
import {
  AuditEventCategory,
  AuditEventCategoryLabel,
  type AuditEventCategoryValue,
  AuditEventLevel,
  AuditEventLevelLabel,
  type AuditEventLevelValue,
} from "../api/types";

const levelClass = (l: AuditEventLevelValue): string => {
  switch (l) {
    case AuditEventLevel.Error:
      return "text-rose-300";
    case AuditEventLevel.Warning:
      return "text-amber-300";
    default:
      return "text-slate-300";
  }
};

const PAGE_SIZE = 50;

export const AuditEventsTab = () => {
  const flows = useQuery({ queryKey: ["smartconnect", "flows"], queryFn: fetchFlows });
  const [filters, setFilters] = useState<AuditEventQuery>({ take: PAGE_SIZE, skip: 0 });
  const events = useQuery({
    queryKey: ["smartconnect", "events", filters],
    queryFn: () => fetchAuditEvents(filters),
    refetchInterval: 30_000,
  });

  const update = (patch: Partial<AuditEventQuery>) =>
    setFilters({ ...filters, skip: 0, ...patch });
  const skip = filters.skip ?? 0;

  const flowName = (id?: string | null) =>
    id ? flows.data?.find((f) => f.id === id)?.name ?? id.slice(0, 8) : "—";

  return (
    <section className="space-y-4">
      <div className="grid grid-cols-1 gap-2 md:grid-cols-5">
        <select
          value={filters.category === undefined ? "" : String(filters.category)}
          onChange={(e) =>
            update({
              category:
                e.target.value === ""
                  ? undefined
                  : (Number(e.target.value) as AuditEventCategoryValue),
            })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        >
          <option value="">Any category</option>
          {Object.entries(AuditEventCategoryLabel).map(([v, label]) => (
            <option key={v} value={v}>{label}</option>
          ))}
        </select>
        <select
          value={filters.level === undefined ? "" : String(filters.level)}
          onChange={(e) =>
            update({
              level:
                e.target.value === ""
                  ? undefined
                  : (Number(e.target.value) as AuditEventLevelValue),
            })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        >
          <option value="">Any level</option>
          {Object.entries(AuditEventLevelLabel).map(([v, label]) => (
            <option key={v} value={v}>{label}</option>
          ))}
        </select>
        <select
          value={filters.flowId ?? ""}
          onChange={(e) => update({ flowId: e.target.value || undefined })}
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        >
          <option value="">All flows</option>
          {flows.data?.map((f) => (
            <option key={f.id} value={f.id}>{f.name}</option>
          ))}
        </select>
        <input
          type="datetime-local"
          value={filters.from?.slice(0, 16) ?? ""}
          onChange={(e) =>
            update({ from: e.target.value ? new Date(e.target.value).toISOString() : undefined })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        />
        <input
          type="datetime-local"
          value={filters.to?.slice(0, 16) ?? ""}
          onChange={(e) =>
            update({ to: e.target.value ? new Date(e.target.value).toISOString() : undefined })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        />
      </div>

      {events.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
      {events.error && <div className="text-xs text-rose-300">Audit service unavailable.</div>}
      {events.data && events.data.length === 0 && (
        <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
          No events match the current filters.
        </div>
      )}
      {events.data && events.data.length > 0 && (
        <div className="overflow-hidden rounded-md border border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-900/60 text-left text-xs uppercase text-slate-400">
              <tr>
                <th className="px-3 py-2">When</th>
                <th className="px-3 py-2">Level</th>
                <th className="px-3 py-2">Category</th>
                <th className="px-3 py-2">Flow</th>
                <th className="px-3 py-2">Actor</th>
                <th className="px-3 py-2">Summary</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {events.data.map((e) => (
                <tr key={e.id} className="align-top">
                  <td className="px-3 py-2 text-xs text-slate-400">
                    {new Date(e.timestamp).toLocaleString()}
                  </td>
                  <td className={"px-3 py-2 text-xs " + levelClass(e.level)}>
                    {AuditEventLevelLabel[e.level] ?? e.level}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-300">
                    {AuditEventCategoryLabel[e.category] ?? e.category}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-400">{flowName(e.flowId)}</td>
                  <td className="px-3 py-2 text-xs text-slate-400">{e.userId ?? "system"}</td>
                  <td className="px-3 py-2 text-xs text-slate-200">
                    {e.summary}
                    {e.attributesJson && (
                      <details className="mt-1 text-slate-500">
                        <summary className="cursor-pointer">attributes</summary>
                        <pre className="mt-1 max-h-40 overflow-auto rounded border border-slate-800 bg-slate-950 p-2 font-mono text-xs">
                          {e.attributesJson}
                        </pre>
                      </details>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex items-center justify-between text-xs text-slate-400">
        <span>page offset {skip}</span>
        <span className="flex gap-2">
          <button
            type="button"
            onClick={() => setFilters({ ...filters, skip: Math.max(0, skip - PAGE_SIZE) })}
            disabled={skip === 0}
            className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
          >
            Prev
          </button>
          <button
            type="button"
            onClick={() => setFilters({ ...filters, skip: skip + PAGE_SIZE })}
            disabled={(events.data?.length ?? 0) < PAGE_SIZE}
            className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
          >
            Next
          </button>
        </span>
      </div>
    </section>
  );
};
