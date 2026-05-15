import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchFlows,
  fetchRecentMessages,
  pauseFlow,
  reprocessMessage,
  startFlow,
  stopFlow,
} from "@/features/smartconnect/api/smartConnectApi";

const stateClass = (state: string): string => {
  const normalized = state.toLowerCase();
  if (normalized.startsWith("started")) return "text-emerald-300";
  if (normalized.startsWith("paused")) return "text-amber-300";
  if (normalized.startsWith("stopped")) return "text-slate-400";
  return "text-slate-300";
};

const FlowActions = ({ flowId, state }: { flowId: string; state: string }) => {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["smartconnect", "flows"] });

  const start = useMutation({ mutationFn: () => startFlow(flowId), onSuccess: invalidate });
  const stop = useMutation({ mutationFn: () => stopFlow(flowId), onSuccess: invalidate });
  const pause = useMutation({ mutationFn: () => pauseFlow(flowId), onSuccess: invalidate });
  const isStarted = state.toLowerCase().startsWith("started");
  const isStopped = state.toLowerCase().startsWith("stopped");

  return (
    <div className="flex gap-1.5">
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
    </div>
  );
};

const MessageActions = ({ messageId }: { messageId: string }) => {
  const queryClient = useQueryClient();
  const reprocess = useMutation({
    mutationFn: () => reprocessMessage(messageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["smartconnect", "messages"] }),
  });
  return (
    <button
      type="button"
      onClick={() => reprocess.mutate()}
      disabled={reprocess.isPending}
      className="rounded-md bg-clinic-600/80 px-2 py-0.5 text-xs text-white hover:bg-clinic-700 disabled:opacity-40"
    >
      {reprocess.isPending ? "…" : "Reprocess"}
    </button>
  );
};

export const IntegrationsPage = () => {
  const flows = useQuery({ queryKey: ["smartconnect", "flows"], queryFn: fetchFlows, refetchInterval: 30_000 });
  const messages = useQuery({
    queryKey: ["smartconnect", "messages"],
    queryFn: () => fetchRecentMessages(25),
    refetchInterval: 10_000,
  });

  return (
    <div className="space-y-6">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">Integrations (SmartConnect)</h2>
        <p className="text-sm text-slate-400">
          Flow runtime + message ledger. Start, pause, stop a flow; reprocess any failed message.
        </p>
      </header>

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h3 className="mb-2 text-sm font-medium text-slate-200">
          Flows {flows.data ? <span className="text-slate-500">({flows.data.length})</span> : null}
        </h3>
        {flows.isLoading && <div className="text-xs text-slate-400">Loading flows…</div>}
        {flows.error && <div className="text-xs text-rose-300">SmartConnect unavailable.</div>}
        {flows.data && flows.data.length === 0 && (
          <div className="text-xs text-slate-500">No flows defined yet.</div>
        )}
        {(flows.data?.length ?? 0) > 0 && flows.data && (
          <ul className="divide-y divide-slate-800 text-sm">
            {flows.data.map((f) => (
              <li key={f.id} className="grid grid-cols-12 items-center gap-3 py-2">
                <div className="col-span-5 text-slate-100">{f.name}</div>
                <div className={`col-span-2 text-xs font-medium ${stateClass(f.state)}`}>{f.state}</div>
                <div className="col-span-3 truncate text-xs text-slate-400">{f.description ?? ""}</div>
                <div className="col-span-2">
                  <FlowActions flowId={f.id} state={f.state} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h3 className="mb-2 text-sm font-medium text-slate-200">Message ledger (latest 25)</h3>
        {messages.isLoading && <div className="text-xs text-slate-400">Loading messages…</div>}
        {messages.error && <div className="text-xs text-rose-300">Could not load the ledger.</div>}
        {messages.data && messages.data.length === 0 && (
          <div className="text-xs text-slate-500">No messages processed yet.</div>
        )}
        {(messages.data?.length ?? 0) > 0 && messages.data && (
          <ul className="divide-y divide-slate-800 text-sm">
            {messages.data.map((m) => (
              <li key={m.id} className="grid grid-cols-12 items-center gap-3 py-2">
                <div className="col-span-3 font-mono text-xs text-slate-300">{m.flowId.slice(0, 8)}</div>
                <div className="col-span-2 text-xs text-slate-400">{m.status}</div>
                <div className="col-span-3 text-xs text-slate-400">
                  {new Date(m.receivedAtUtc).toLocaleString()}
                </div>
                <div className="col-span-2 font-mono text-xs text-slate-500">
                  {m.correlationId?.slice(0, 8) ?? ""}
                </div>
                <div className="col-span-2">
                  <MessageActions messageId={m.id} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
};
