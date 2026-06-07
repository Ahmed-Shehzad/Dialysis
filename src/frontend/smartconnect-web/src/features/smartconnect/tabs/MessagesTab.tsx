import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { fetchFlows } from "../api/flows";
import {
  EXPORT_FORMATS,
  type ExportFormat,
  decodePayloadSnapshot,
  exportMessageDocument,
  fetchMessage,
  fetchMessages,
  reprocessMessage,
} from "../api/messages";
import { OutboundConcurrencyTimeline } from "../components/OutboundConcurrencyTimeline";
import {
  BATCH_METADATA_KEYS,
  type MessageLedgerEntry,
  MessageLedgerStatus,
  MessageLedgerStatusLabel,
  type MessageLedgerStatusValue,
  type MessageListQuery,
} from "../api/types";

const STATUS_OPTIONS: MessageLedgerStatusValue[] = [
  MessageLedgerStatus.Received,
  MessageLedgerStatus.RouteFilterDropped,
  MessageLedgerStatus.OutboundSent,
  MessageLedgerStatus.OutboundFailed,
  MessageLedgerStatus.Completed,
];

const statusClass = (s: MessageLedgerStatusValue): string => {
  switch (s) {
    case MessageLedgerStatus.OutboundSent:
    case MessageLedgerStatus.Completed:
      return "text-emerald-300";
    case MessageLedgerStatus.OutboundFailed:
      return "text-rose-300";
    case MessageLedgerStatus.RouteFilterDropped:
      return "text-amber-300";
    default:
      return "text-slate-300";
  }
};

const MessageDrawer = ({ entryId, onClose }: { entryId: string; onClose: () => void }) => {
  const queryClient = useQueryClient();
  const entry = useQuery({
    queryKey: ["smartconnect", "messages", entryId],
    queryFn: () => fetchMessage(entryId),
  });
  const reprocess = useMutation({
    mutationFn: () => reprocessMessage(entryId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "messages"] });
    },
  });
  const [exportFormat, setExportFormat] = useState<ExportFormat>("cda");
  const exportDoc = useMutation({
    mutationFn: () => exportMessageDocument(entryId, exportFormat),
  });
  const text = useMemo(
    () => decodePayloadSnapshot(entry.data?.payloadSnapshot),
    [entry.data?.payloadSnapshot],
  );
  const hasPayload = Boolean(text);

  // Sibling ledger entries for the same inbound message — feeds the concurrency timeline.
  // Heuristic fetch: pull the recent ledger window for the same flow and filter client-side.
  // Good enough for the in-drawer Gantt; a dedicated /messages/{messageId}/timeline endpoint
  // is a clean follow-up if usage proves heavy.
  const siblings = useQuery({
    enabled: entry.data !== undefined,
    queryKey: [
      "smartconnect",
      "messages",
      "siblings",
      entry.data?.flowId,
      entry.data?.integrationMessageId,
    ],
    queryFn: async () => {
      if (!entry.data) return [];
      const res = await fetchMessages({ flowId: entry.data.flowId, take: 200 });
      return res.items.filter((i) => i.integrationMessageId === entry.data!.integrationMessageId);
    },
  });

  return (
    <div className="fixed inset-0 z-40 flex" role="dialog" aria-modal="true">
      <button
        type="button"
        aria-label="Close drawer"
        onClick={onClose}
        className="flex-1 bg-black/40"
      />
      <aside className="w-full max-w-2xl overflow-y-auto border-l border-slate-800 bg-slate-950 p-5 shadow-2xl">
        <header className="mb-3 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-clinic-100">Ledger entry</h3>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
          >
            Close
          </button>
        </header>
        {entry.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
        {entry.error && <div className="text-xs text-rose-300">Could not load entry.</div>}
        {entry.data && (
          <div className="space-y-4 text-sm">
            <dl className="grid grid-cols-3 gap-x-3 gap-y-1 text-xs">
              <dt className="text-slate-500">Entry id</dt>
              <dd className="col-span-2 font-mono text-slate-300">{entry.data.id}</dd>
              <dt className="text-slate-500">Flow</dt>
              <dd className="col-span-2 font-mono text-slate-300">{entry.data.flowId}</dd>
              <dt className="text-slate-500">Message id</dt>
              <dd className="col-span-2 font-mono text-slate-300">
                {entry.data.integrationMessageId}
              </dd>
              <dt className="text-slate-500">Correlation</dt>
              <dd className="col-span-2 font-mono text-slate-300">{entry.data.correlationId}</dd>
              <dt className="text-slate-500">Status</dt>
              <dd className={"col-span-2 " + statusClass(entry.data.status)}>
                {MessageLedgerStatusLabel[entry.data.status] ?? entry.data.status}
              </dd>
              <dt className="text-slate-500">Route ordinal</dt>
              <dd className="col-span-2 text-slate-300">
                {entry.data.outboundRouteOrdinal ?? "—"}
              </dd>
              <dt className="text-slate-500">When</dt>
              <dd className="col-span-2 text-slate-300">
                {new Date(entry.data.createdAtUtc).toLocaleString()}
              </dd>
            </dl>

            {entry.data.detail && (
              <div>
                <div className="mb-1 text-xs uppercase text-slate-500">Detail</div>
                <pre className="max-h-40 overflow-auto rounded-md border border-slate-800 bg-slate-900/40 p-2 text-xs text-rose-200">
                  {entry.data.detail}
                </pre>
              </div>
            )}

            {siblings.data && siblings.data.length > 1 && (
              <div className="rounded-md border border-slate-800 bg-slate-900/40 p-3">
                <OutboundConcurrencyTimeline entries={siblings.data} />
              </div>
            )}

            <div>
              <div className="mb-1 text-xs uppercase text-slate-500">Payload snapshot</div>
              {text ? (
                <pre className="max-h-96 overflow-auto rounded-md border border-slate-800 bg-slate-900/40 p-2 font-mono text-xs text-slate-200">
                  {text}
                </pre>
              ) : (
                <div className="text-xs text-slate-500">
                  (none — payload snapshots are recorded only at the Received and OutboundFailed
                  stages)
                </div>
              )}
            </div>

            <div className="flex flex-wrap items-center gap-2 border-t border-slate-800 pt-3">
              <button
                type="button"
                onClick={() => reprocess.mutate()}
                disabled={reprocess.isPending}
                className="rounded-md bg-clinic-600 px-3 py-1 text-xs font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
              >
                {reprocess.isPending ? "Submitting…" : "Reprocess"}
              </button>
              {reprocess.data && (
                <span className="text-xs text-emerald-300">
                  Resubmitted as{" "}
                  <code className="font-mono">{reprocess.data.reprocessedMessageId}</code>
                </span>
              )}
              {reprocess.error && <span className="text-xs text-rose-300">Reprocess failed.</span>}
            </div>

            <div className="flex flex-wrap items-center gap-2 border-t border-slate-800 pt-3">
              <span className="text-xs uppercase text-slate-500">Export as</span>
              <select
                aria-label="Export document format"
                title="Export document format"
                value={exportFormat}
                onChange={(e) => setExportFormat(e.target.value as ExportFormat)}
                disabled={!hasPayload}
                className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200 disabled:opacity-40"
              >
                {EXPORT_FORMATS.map((f) => (
                  <option key={f.value} value={f.value}>
                    {f.label}
                  </option>
                ))}
              </select>
              <button
                type="button"
                onClick={() => exportDoc.mutate()}
                disabled={!hasPayload || exportDoc.isPending}
                className="rounded-md border border-slate-700 px-3 py-1 text-xs font-medium text-slate-200 hover:bg-slate-800 disabled:opacity-40"
              >
                {exportDoc.isPending ? "Preparing…" : "Download"}
              </button>
              {!hasPayload && (
                <span className="text-xs text-slate-500">(no captured payload to convert)</span>
              )}
              {exportDoc.error && <span className="text-xs text-rose-300">Export failed.</span>}
            </div>
          </div>
        )}
      </aside>
    </div>
  );
};

const PAGE_SIZE = 25;

export const MessagesTab = () => {
  const flows = useQuery({ queryKey: ["smartconnect", "flows"], queryFn: fetchFlows });
  const [filters, setFilters] = useState<MessageListQuery>({ take: PAGE_SIZE, skip: 0 });
  const [drawerId, setDrawerId] = useState<string | null>(null);

  const list = useQuery({
    queryKey: ["smartconnect", "messages", filters],
    queryFn: () => fetchMessages(filters),
    refetchInterval: 10_000,
  });

  const flowName = (id: string) => flows.data?.find((f) => f.id === id)?.name ?? id.slice(0, 8);

  const update = (patch: Partial<MessageListQuery>) =>
    setFilters({ ...filters, skip: 0, ...patch });

  const page = filters.skip ?? 0;
  const totalCount = list.data?.totalCount ?? 0;
  const next = () => setFilters({ ...filters, skip: page + PAGE_SIZE });
  const prev = () => setFilters({ ...filters, skip: Math.max(0, page - PAGE_SIZE) });

  return (
    <section className="space-y-4">
      <div className="grid grid-cols-1 gap-2 md:grid-cols-8">
        <select
          aria-label="Filter by flow"
          value={filters.flowId ?? ""}
          onChange={(e) => update({ flowId: e.target.value || undefined })}
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        >
          <option value="">All flows</option>
          {flows.data?.map((f) => (
            <option key={f.id} value={f.id}>
              {f.name}
            </option>
          ))}
        </select>
        <select
          aria-label="Filter by status"
          value={filters.status === undefined ? "" : String(filters.status)}
          onChange={(e) =>
            update({
              status:
                e.target.value === ""
                  ? undefined
                  : (Number(e.target.value) as MessageLedgerStatusValue),
            })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        >
          <option value="">Any status</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {MessageLedgerStatusLabel[s]}
            </option>
          ))}
        </select>
        <input
          aria-label="Correlation prefix"
          placeholder="Correlation prefix"
          value={filters.correlationIdPrefix ?? ""}
          onChange={(e) => update({ correlationIdPrefix: e.target.value || undefined })}
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200 placeholder-slate-500"
        />
        <input
          placeholder="Message type (e.g. ORU^R01)"
          value={filters.messageType ?? ""}
          onChange={(e) => update({ messageType: e.target.value || undefined })}
          title="Exact match on the derived MSH-9 ledger column"
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200 placeholder-slate-500"
        />
        <input
          placeholder="Sender (e.g. MachineA@FACILITY)"
          value={filters.senderId ?? ""}
          onChange={(e) => update({ senderId: e.target.value || undefined })}
          title="Exact match on the derived sender ledger column (sendingApp@sendingFacility for HL7)"
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200 placeholder-slate-500"
        />
        <input
          placeholder="Batch ID (e.g. file:/in/labs.csv)"
          value={filters.batchId ?? ""}
          onChange={(e) => update({ batchId: e.target.value || undefined })}
          title="Exact match on the derived batch-id ledger column — every record fanned out from a single source shares one batch id"
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200 placeholder-slate-500"
        />
        <input
          aria-label="From date"
          type="datetime-local"
          value={filters.from?.slice(0, 16) ?? ""}
          onChange={(e) =>
            update({ from: e.target.value ? new Date(e.target.value).toISOString() : undefined })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        />
        <input
          aria-label="To date"
          type="datetime-local"
          value={filters.to?.slice(0, 16) ?? ""}
          onChange={(e) =>
            update({ to: e.target.value ? new Date(e.target.value).toISOString() : undefined })
          }
          className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        />
      </div>

      {list.isLoading && <div className="text-xs text-slate-400">Loading messages…</div>}
      {list.error && <div className="text-xs text-rose-300">Ledger unavailable.</div>}
      {list.data?.items.length === 0 && (
        <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
          No ledger entries match the current filters.
        </div>
      )}

      {list.data && list.data.items.length > 0 && (
        <>
          <div className="overflow-hidden rounded-md border border-slate-800">
            <table className="w-full text-sm">
              <thead className="bg-slate-900/60 text-left text-xs uppercase text-slate-400">
                <tr>
                  <th className="px-3 py-2">When</th>
                  <th className="px-3 py-2">Flow</th>
                  <th className="px-3 py-2">Status</th>
                  <th className="px-3 py-2">Route</th>
                  <th className="px-3 py-2">Correlation</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {list.data.items.map((m: MessageLedgerEntry) => (
                  <tr
                    key={m.id}
                    className="cursor-pointer hover:bg-slate-900/30"
                    onClick={() => setDrawerId(m.id)}
                  >
                    <td className="px-3 py-2 text-xs text-slate-300">
                      {new Date(m.createdAtUtc).toLocaleTimeString()}
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-300">{flowName(m.flowId)}</td>
                    <td className={"px-3 py-2 text-xs " + statusClass(m.status)}>
                      {MessageLedgerStatusLabel[m.status] ?? m.status}
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-400">
                      {m.outboundRouteOrdinal ?? "—"}
                    </td>
                    <td className="px-3 py-2 font-mono text-xs text-slate-400">
                      <div className="flex items-center gap-2">
                        <span>{m.correlationId}</span>
                        <BatchBadge entry={m} onFilter={(batchId) => update({ batchId })} />
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>
              Showing {page + 1}–{Math.min(page + list.data.items.length, totalCount)} of{" "}
              {totalCount}
            </span>
            <span className="flex gap-2">
              <button
                type="button"
                onClick={prev}
                disabled={page === 0}
                className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
              >
                Prev
              </button>
              <button
                type="button"
                onClick={next}
                disabled={page + list.data.items.length >= totalCount}
                className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
              >
                Next
              </button>
            </span>
          </div>
        </>
      )}

      {drawerId && <MessageDrawer entryId={drawerId} onClose={() => setDrawerId(null)} />}
    </section>
  );
};

/**
 * Slice D2 affordance — compact "batch n/total" pill that surfaces batch context inline on
 * each ledger row. Click filters the grid to every message in the same batch via the
 * indexed `BatchId` column. Renders nothing when the row carries no batch metadata.
 */
const BatchBadge = ({
  entry,
  onFilter,
}: {
  entry: MessageLedgerEntry;
  onFilter: (batchId: string) => void;
}) => {
  const meta = entry.metadata;
  if (!meta) return null;
  const batchId = meta[BATCH_METADATA_KEYS.BatchId];
  if (!batchId) return null;
  const sequence = meta[BATCH_METADATA_KEYS.Sequence];
  const total = meta[BATCH_METADATA_KEYS.Total];
  return (
    <button
      type="button"
      title={`Filter to batch: ${batchId}`}
      onClick={(e) => {
        e.stopPropagation();
        onFilter(batchId);
      }}
      className="rounded-full border border-slate-700 bg-slate-800/60 px-2 py-0.5 text-[10px] font-normal text-slate-300 hover:bg-slate-700 hover:text-slate-100"
    >
      {sequence && total ? `batch ${sequence}/${total}` : "batch"}
    </button>
  );
};
