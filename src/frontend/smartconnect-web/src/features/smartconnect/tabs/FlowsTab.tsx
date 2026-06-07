import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { NewChannelDialog } from "../components/NewChannelDialog";
import {
  deleteFlow,
  exportFlow,
  fetchFlowStatistics,
  fetchFlows,
  importFlow,
  pauseFlow,
  startFlow,
  stopFlow,
} from "../api/flows";
import { fetchGroups } from "../api/groups";
import {
  FlowRuntimeState,
  FlowRuntimeStateLabel,
  type FlowRuntimeStateValue,
  type FlowStatusCount,
  type IntegrationFlow,
  MessageLedgerStatus,
  MessageLedgerStatusLabel,
} from "../api/types";

const stateBadgeClass = (state: FlowRuntimeStateValue): string => {
  switch (state) {
    case FlowRuntimeState.Started:
      return "bg-emerald-700/40 text-emerald-200 border-emerald-700";
    case FlowRuntimeState.Paused:
      return "bg-amber-700/40 text-amber-200 border-amber-700";
    case FlowRuntimeState.Stopped:
    default:
      return "bg-slate-700/40 text-slate-300 border-slate-700";
  }
};

const FlowStatistics = ({ flowId }: { flowId: string }) => {
  const stats = useQuery({
    queryKey: ["smartconnect", "flows", flowId, "statistics"],
    queryFn: () => fetchFlowStatistics(flowId),
    refetchInterval: 15_000,
  });
  if (stats.isLoading) return <span className="text-xs text-slate-500">…</span>;
  if (stats.error || !stats.data?.length) return <span className="text-xs text-slate-500">—</span>;
  const byStatus = new Map<number, number>(
    stats.data.map((s: FlowStatusCount) => [s.status, s.count]),
  );
  const cell = (status: number) => byStatus.get(status) ?? 0;
  return (
    <div className="flex flex-wrap gap-3 text-xs">
      <span title="Received">
        <span className="text-slate-500">rx </span>
        <span className="text-slate-200">{cell(MessageLedgerStatus.Received)}</span>
      </span>
      <span title="Outbound sent">
        <span className="text-slate-500">sent </span>
        <span className="text-emerald-200">{cell(MessageLedgerStatus.OutboundSent)}</span>
      </span>
      <span title="Outbound failed">
        <span className="text-slate-500">fail </span>
        <span className="text-rose-200">{cell(MessageLedgerStatus.OutboundFailed)}</span>
      </span>
      <span title="Route-filter dropped">
        <span className="text-slate-500">drop </span>
        <span className="text-amber-200">{cell(MessageLedgerStatus.RouteFilterDropped)}</span>
      </span>
      <span title="Completed">
        <span className="text-slate-500">done </span>
        <span className="text-clinic-200">{cell(MessageLedgerStatus.Completed)}</span>
      </span>
    </div>
  );
};

const FlowActions = ({ flow }: { flow: IntegrationFlow }) => {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["smartconnect", "flows"] });
  const start = useMutation({ mutationFn: () => startFlow(flow.id), onSuccess: invalidate });
  const stop = useMutation({ mutationFn: () => stopFlow(flow.id), onSuccess: invalidate });
  const pause = useMutation({ mutationFn: () => pauseFlow(flow.id), onSuccess: invalidate });
  const remove = useMutation({
    mutationFn: () => deleteFlow(flow.id),
    onSuccess: invalidate,
  });
  const isStarted = flow.runtimeState === FlowRuntimeState.Started;
  const isStopped = flow.runtimeState === FlowRuntimeState.Stopped;

  const onExport = async () => {
    const data = await exportFlow(flow.id);
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `flow-${flow.name.replace(/\W+/g, "_")}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const onDelete = () => {
    if (confirm(`Delete flow "${flow.name}"? This cannot be undone.`)) remove.mutate();
  };

  return (
    <div className="flex flex-wrap gap-1.5">
      <button
        type="button"
        onClick={() => start.mutate()}
        disabled={isStarted || start.isPending}
        className="rounded-md bg-emerald-600/80 px-2 py-0.5 text-xs text-white hover:bg-emerald-700 disabled:opacity-40"
      >
        Start
      </button>
      <button
        type="button"
        onClick={() => pause.mutate()}
        disabled={!isStarted || pause.isPending}
        className="rounded-md bg-amber-600/80 px-2 py-0.5 text-xs text-white hover:bg-amber-700 disabled:opacity-40"
      >
        Pause
      </button>
      <button
        type="button"
        onClick={() => stop.mutate()}
        disabled={isStopped || stop.isPending}
        className="rounded-md bg-rose-600/80 px-2 py-0.5 text-xs text-white hover:bg-rose-700 disabled:opacity-40"
      >
        Stop
      </button>
      <Link
        to={`/integrations/editor/${flow.id}`}
        className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
        title="Open visual pipeline editor"
      >
        Edit
      </Link>
      <button
        type="button"
        onClick={onExport}
        className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
        title="Download flow JSON"
      >
        Export
      </button>
      <button
        type="button"
        onClick={onDelete}
        disabled={remove.isPending}
        className="rounded-md border border-rose-700 px-2 py-0.5 text-xs text-rose-300 hover:bg-rose-900/40 disabled:opacity-40"
      >
        Delete
      </button>
    </div>
  );
};

const ImportFlowButton = () => {
  const queryClient = useQueryClient();
  const mut = useMutation({
    mutationFn: (flow: IntegrationFlow) => importFlow(flow),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["smartconnect", "flows"] }),
  });
  const onFile = async (file: File) => {
    const text = await file.text();
    try {
      const parsed = JSON.parse(text) as IntegrationFlow;
      mut.mutate(parsed);
    } catch {
      alert("File is not valid JSON.");
    }
  };
  return (
    <label className="cursor-pointer rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800">
      {mut.isPending ? "Importing…" : "Import flow"}
      <input
        type="file"
        accept="application/json"
        className="hidden"
        onChange={(e) => {
          const f = e.target.files?.[0];
          if (f) void onFile(f);
          e.target.value = "";
        }}
      />
    </label>
  );
};

export const FlowsTab = () => {
  const flows = useQuery({
    queryKey: ["smartconnect", "flows"],
    queryFn: fetchFlows,
    refetchInterval: 30_000,
  });
  const groups = useQuery({ queryKey: ["smartconnect", "groups"], queryFn: fetchGroups });
  const [groupFilter, setGroupFilter] = useState<string>("");
  const [stateFilter, setStateFilter] = useState<string>("");
  const [newChannelOpen, setNewChannelOpen] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();

  // The Integrations page's "New channel" quick action lands here with ?action=new; open the
  // dialog when that param is present (the ?tab=flows part is preserved).
  const newChannelRequested = searchParams.get("action") === "new";
  useEffect(() => {
    if (newChannelRequested) setNewChannelOpen(true);
  }, [newChannelRequested]);

  const closeNewChannel = () => {
    setNewChannelOpen(false);
    if (newChannelRequested) {
      const next = new URLSearchParams(searchParams);
      next.delete("action");
      setSearchParams(next, { replace: true });
    }
  };

  const filtered = useMemo(() => {
    let items = flows.data ?? [];
    if (groupFilter) items = items.filter((f) => (f.groupId ?? "") === groupFilter);
    if (stateFilter) items = items.filter((f) => String(f.runtimeState) === stateFilter);
    return items;
  }, [flows.data, groupFilter, stateFilter]);

  const groupName = (id?: string | null) => groups.data?.find((g) => g.id === id)?.name ?? "—";

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-medium text-slate-200">
          Flows{" "}
          {flows.data ? (
            <span className="text-slate-500">
              ({filtered.length}/{flows.data.length})
            </span>
          ) : null}
        </h3>
        <div className="flex flex-wrap items-center gap-2 text-xs">
          <select
            aria-label="Filter by group"
            value={groupFilter}
            onChange={(e) => setGroupFilter(e.target.value)}
            className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-slate-200"
          >
            <option value="">All groups</option>
            {groups.data?.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name}
              </option>
            ))}
          </select>
          <select
            aria-label="Filter by state"
            value={stateFilter}
            onChange={(e) => setStateFilter(e.target.value)}
            className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-slate-200"
          >
            <option value="">All states</option>
            <option value={String(FlowRuntimeState.Started)}>Started</option>
            <option value={String(FlowRuntimeState.Paused)}>Paused</option>
            <option value={String(FlowRuntimeState.Stopped)}>Stopped</option>
          </select>
          <ImportFlowButton />
          <button
            type="button"
            onClick={() => setNewChannelOpen(true)}
            className="rounded-md bg-clinic-600 px-3 py-1 text-xs font-medium text-white hover:bg-clinic-700"
          >
            + New channel
          </button>
        </div>
      </div>

      {newChannelOpen && <NewChannelDialog onClose={closeNewChannel} />}

      {flows.isLoading && <div className="text-xs text-slate-400">Loading flows…</div>}
      {flows.error && <div className="text-xs text-rose-300">SmartConnect unavailable.</div>}
      {flows.data && filtered.length === 0 && (
        <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
          No flows match the current filters.
        </div>
      )}

      {filtered.length > 0 && (
        <div className="overflow-hidden rounded-md border border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-900/60 text-left text-xs uppercase text-slate-400">
              <tr>
                <th className="px-3 py-2">Name</th>
                <th className="px-3 py-2">State</th>
                <th className="px-3 py-2">Group</th>
                <th className="px-3 py-2">Last 24h activity</th>
                <th className="px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {filtered.map((f) => (
                <tr key={f.id} className="hover:bg-slate-900/30">
                  <td className="px-3 py-2 align-top">
                    <div className="text-slate-100">{f.name}</div>
                    {f.description && <div className="text-xs text-slate-500">{f.description}</div>}
                    {f.tags?.length ? (
                      <div className="mt-1 flex flex-wrap gap-1">
                        {f.tags.map((t) => (
                          <span
                            key={t}
                            className="rounded bg-slate-800/70 px-1.5 py-0.5 text-[10px] text-slate-400"
                          >
                            {t}
                          </span>
                        ))}
                      </div>
                    ) : null}
                  </td>
                  <td className="px-3 py-2 align-top">
                    <span
                      className={
                        "inline-block rounded border px-2 py-0.5 text-xs font-medium " +
                        stateBadgeClass(f.runtimeState)
                      }
                    >
                      {FlowRuntimeStateLabel[f.runtimeState] ?? "Unknown"}
                    </span>
                  </td>
                  <td className="px-3 py-2 align-top text-xs text-slate-400">
                    {groupName(f.groupId)}
                  </td>
                  <td className="px-3 py-2 align-top">
                    <FlowStatistics flowId={f.id} />
                  </td>
                  <td className="px-3 py-2 align-top text-right">
                    <FlowActions flow={f} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="text-xs text-slate-500">
        Statuses match the Mirth-style ledger ({Object.values(MessageLedgerStatusLabel).join(" · ")}
        ). Counts come from
        <code className="px-1 text-slate-400">/flows/{"{id}"}/statistics</code> and refresh every
        15s.
      </p>
    </section>
  );
};
