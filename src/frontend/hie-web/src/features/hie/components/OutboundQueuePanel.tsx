import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchOutboundBundles,
  outboundStatusLabel,
  retryOutboundBundle,
  type OutboundBundleDto,
  type OutboundBundleStatus,
} from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { PatientLabel } from "@/features/patients/PatientLabel";

const TONE_BY_STATUS: Record<OutboundBundleStatus, string> = {
  1: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  2: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100",
  3: "border-rose-700/70 bg-rose-950/40 text-rose-100",
};

const FILTERS: { value: OutboundBundleStatus | null; label: string }[] = [
  { value: null, label: "All" },
  { value: 1, label: "Pending" },
  { value: 2, label: "Delivered" },
  { value: 3, label: "Failed" },
];

const formatTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleTimeString() : "—";

/**
 * Operator view of the FHIR outbound dispatch queue. Lists OutboundBundle rows ordered
 * most-recent first with a status filter (All / Pending / Delivered / Failed) and a
 * Retry action on every non-Delivered row. Retry calls the new ops endpoint which is
 * idempotent at the aggregate level — re-clicking a row already Pending is harmless,
 * already-Delivered bundles are absorbed silently by the domain.
 */
export const OutboundQueuePanel = () => {
  const [statusFilter, setStatusFilter] = useState<OutboundBundleStatus | null>(null);
  const queryClient = useQueryClient();

  const bundles = useQuery({
    queryKey: ["hie", "ops", "outbound", statusFilter],
    queryFn: () => fetchOutboundBundles(statusFilter),
    refetchInterval: 15_000,
    staleTime: 10_000,
  });

  const retry = useMutation({
    mutationFn: retryOutboundBundle,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["hie", "ops", "outbound"] });
    },
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h3 className="text-sm font-medium text-slate-200">Outbound dispatch queue</h3>
          <p className="text-xs text-slate-400">
            FHIR resources queued for delivery to partner endpoints. Failed rows retry on demand.
          </p>
        </div>
        <div className="flex items-center gap-1 text-xs">
          {FILTERS.map((f) => (
            <button
              key={f.label}
              type="button"
              onClick={() => setStatusFilter(f.value)}
              className={`rounded-md border px-2 py-1 transition ${
                statusFilter === f.value
                  ? "border-clinic-500 bg-clinic-900/40 text-clinic-100"
                  : "border-slate-700 text-slate-300 hover:border-slate-500"
              }`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </header>

      {bundles.isLoading && <div className="text-xs text-slate-400">Loading queue…</div>}

      {bundles.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(bundles.error)}
        </div>
      )}

      {bundles.data && bundles.data.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No bundles{statusFilter !== null ? ` in ${outboundStatusLabel(statusFilter)} state` : ""}.
        </div>
      )}

      {bundles.data && bundles.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {bundles.data.map((b) => (
            <OutboundRow
              key={b.id}
              bundle={b}
              onRetry={() => retry.mutate(b.id)}
              retryPending={retry.isPending}
            />
          ))}
        </ul>
      )}

      {retry.error && (
        <div role="alert" className="text-xs text-rose-300">
          {humanizeError(retry.error)}
        </div>
      )}
    </section>
  );
};

const OutboundRow = ({
  bundle,
  onRetry,
  retryPending,
}: {
  bundle: OutboundBundleDto;
  onRetry: () => void;
  retryPending: boolean;
}) => (
  <li className="grid grid-cols-12 items-center gap-2 py-2">
    <span className="col-span-2 truncate font-mono text-xs text-slate-300" title={bundle.partnerId}>
      {bundle.partnerId}
    </span>
    <span className="col-span-2 truncate text-xs text-slate-200">{bundle.resourceType}</span>
    <span className="col-span-3 min-w-0 text-xs">
      <span className="block truncate font-mono text-slate-400" title={bundle.logicalId}>
        {bundle.logicalId}
      </span>
      <PatientLabel
        patientId={bundle.patientId}
        showMrn={false}
        className="block truncate text-[11px] text-slate-300"
      />
    </span>
    <span className="col-span-1">
      <span className={`rounded-full border px-2 py-0.5 text-xs ${TONE_BY_STATUS[bundle.status]}`}>
        {outboundStatusLabel(bundle.status)}
      </span>
    </span>
    <span className="col-span-1 text-xs text-slate-400" title={`attempts: ${bundle.attempts}`}>
      ×{bundle.attempts}
    </span>
    <span className="col-span-2 text-xs text-slate-400" title={bundle.nextAttemptAtUtc}>
      {bundle.status === 2
        ? formatTime(bundle.deliveredAtUtc)
        : formatTime(bundle.nextAttemptAtUtc)}
    </span>
    <span className="col-span-1 text-right">
      {bundle.status !== 2 && (
        <button
          type="button"
          onClick={onRetry}
          disabled={retryPending}
          className="rounded-md border border-clinic-700/60 px-2 py-0.5 text-xs text-clinic-200 transition hover:border-clinic-500 disabled:opacity-50"
        >
          Retry
        </button>
      )}
    </span>
    {bundle.lastFailureReason && (
      <span className="col-span-12 pl-2 text-xs text-rose-300/80" title="Last failure reason">
        {bundle.lastFailureReason}
      </span>
    )}
  </li>
);
