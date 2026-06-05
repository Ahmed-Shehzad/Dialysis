import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  fetchLabOrder,
  fetchLabOrdersByPatient,
  type LabObservation,
  type LabOrderStatus,
  type LabOrderSummary,
  type LabResultInterpretation,
} from "@/features/lab/api/labApi";
import { humanizeError } from "@/lib/api/humanizeError";

const formatDateTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleString() : "—";

const STATUS_TONE: Record<string, string> = {
  Placed: "border-slate-700 bg-slate-900/40 text-slate-300",
  Transmitted: "border-sky-700/70 bg-sky-950/30 text-sky-100",
  InProgress: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  Resulted: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100",
  Cancelled: "border-rose-700/70 bg-rose-950/40 text-rose-100",
};

const statusTone = (status: LabOrderStatus): string =>
  STATUS_TONE[status] ?? "border-slate-700 bg-slate-900/40 text-slate-300";

// Abnormal interpretations get tone so a clinician spots out-of-range results at a glance.
const ABNORMAL: Record<string, string> = {
  Low: "text-amber-200",
  High: "text-amber-200",
  CriticalLow: "text-rose-300 font-semibold",
  CriticalHigh: "text-rose-300 font-semibold",
  Abnormal: "text-amber-200",
};

const interpretationTone = (interp: LabResultInterpretation): string =>
  ABNORMAL[interp] ?? "text-slate-300";

/**
 * Chart panel for the dedicated Lab bounded context (reached via the EHR BFF's _x/lab
 * aggregation). Lists the patient's lab orders most-recent first; expanding a resulted order
 * loads its observation lines (value / unit / reference range) with abnormal flags toned.
 *
 * This is the Lab context's own view — distinct from EHR's in-house lab ordering. It is
 * read-only: orders are placed elsewhere and results arrive from the LIS via SmartConnect.
 */
export const LabResultsPanel = ({ patientId }: { patientId: string }) => {
  const orders = useQuery({
    queryKey: ["lab", "orders", patientId],
    queryFn: () => fetchLabOrdersByPatient(patientId),
    enabled: Boolean(patientId),
    refetchInterval: 30_000,
  });

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="mb-2">
        <h3 className="text-sm font-medium text-slate-200">
          Laboratory <span className="text-slate-500">(LIS orders &amp; results)</span>
        </h3>
        <p className="text-xs text-slate-400">
          Orders transmitted to the Laboratory Information System and the results returned for this
          patient.
        </p>
      </header>

      {orders.isLoading && <p className="text-xs text-slate-400">Loading lab orders…</p>}
      {orders.error && (
        <p role="alert" className="text-xs text-amber-300">
          {humanizeError(orders.error)}
        </p>
      )}
      {orders.data && orders.data.length === 0 && (
        <p className="text-xs text-slate-500">No lab orders on file for this patient.</p>
      )}

      {orders.data && orders.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {orders.data.map((order) => (
            <LabOrderRow key={order.id} order={order} />
          ))}
        </ul>
      )}
    </section>
  );
};

const LabOrderRow = ({ order }: { order: LabOrderSummary }) => {
  const [expanded, setExpanded] = useState(false);
  const resulted = order.status === "Resulted";

  const detail = useQuery({
    queryKey: ["lab", "order", order.id],
    queryFn: () => fetchLabOrder(order.id),
    enabled: expanded,
  });

  return (
    <li className="py-2">
      <button
        type="button"
        onClick={() => setExpanded((v) => !v)}
        className="grid w-full grid-cols-12 items-center gap-2 text-left"
        aria-expanded={expanded}
      >
        <span
          className="col-span-4 truncate font-mono text-xs text-slate-300"
          title={order.placerOrderNumber}
        >
          {order.placerOrderNumber}
        </span>
        <span className="col-span-2 text-xs text-slate-400">
          {order.testCount} test{order.testCount === 1 ? "" : "s"}
        </span>
        <span className="col-span-2 text-xs text-slate-400">
          {order.priority === "Stat" ? <span className="text-rose-300">STAT</span> : "Routine"}
        </span>
        <span className="col-span-3">
          <span className={`rounded-full border px-2 py-0.5 text-xs ${statusTone(order.status)}`}>
            {order.status}
          </span>
        </span>
        <span className="col-span-1 text-right text-xs text-slate-500">{expanded ? "▾" : "▸"}</span>
      </button>

      <div className="mt-1 text-xs text-slate-500">
        Placed {formatDateTime(order.placedAtUtc)}
        {resulted && ` · resulted ${formatDateTime(order.resultedAtUtc)}`}
      </div>

      {expanded && (
        <div className="mt-2 rounded-md border border-slate-800 bg-slate-950/40 p-2">
          {detail.isLoading && <p className="text-xs text-slate-400">Loading results…</p>}
          {detail.error && (
            <p role="alert" className="text-xs text-amber-300">
              {humanizeError(detail.error)}
            </p>
          )}
          {detail.data && detail.data.results.length === 0 && (
            <p className="text-xs text-slate-500">
              No observations yet — awaiting results from the LIS.
            </p>
          )}
          {detail.data && detail.data.results.length > 0 && (
            <ul className="divide-y divide-slate-800/60">
              {detail.data.results.map((obs, i) => (
                <ObservationRow key={`${obs.loincCode}-${i}`} obs={obs} />
              ))}
            </ul>
          )}
        </div>
      )}
    </li>
  );
};

const ObservationRow = ({ obs }: { obs: LabObservation }) => (
  <li className="grid grid-cols-12 items-center gap-2 py-1.5 text-xs">
    <span className="col-span-5 truncate text-slate-200" title={obs.loincCode}>
      {obs.display}
    </span>
    <span className={`col-span-3 ${interpretationTone(obs.interpretation)}`}>
      {obs.value}
      {obs.unit ? <span className="ml-1 text-slate-500">{obs.unit}</span> : null}
    </span>
    <span className="col-span-2 text-slate-500">{obs.referenceRange ?? "—"}</span>
    <span className={`col-span-2 text-right ${interpretationTone(obs.interpretation)}`}>
      {obs.interpretation === "Normal" ? "" : obs.interpretation}
    </span>
  </li>
);
