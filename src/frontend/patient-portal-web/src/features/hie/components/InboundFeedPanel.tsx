import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchInboundResources, type InboundResourceDto } from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

const formatDateTime = (iso: string): string => new Date(iso).toLocaleString();

/**
 * Operator view of inbound FHIR receipts. Renders the latest delivered resources from
 * partner POSTs (`/fhir/{Type}` handled by the TEFCA-gated FhirController). Polls every
 * 15 s and supports a free-text partner filter so the operator can narrow to one trading
 * partner during a delivery audit.
 *
 * `validationOutcome` is rendered as a tone-coded chip when present — partners that fail
 * US Core conformance will be visible at a glance.
 */
export const InboundFeedPanel = () => {
  const [partnerFilter, setPartnerFilter] = useState("");
  const partnerArg = useMemo(
    () => (partnerFilter.trim().length === 0 ? null : partnerFilter.trim()),
    [partnerFilter],
  );

  const rows = useQuery({
    queryKey: ["hie", "ops", "inbound", partnerArg],
    queryFn: () => fetchInboundResources(partnerArg),
    refetchInterval: 15_000,
    staleTime: 10_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h3 className="text-sm font-medium text-slate-200">Inbound FHIR feed</h3>
          <p className="text-xs text-slate-400">
            Most-recent resources accepted from partner POSTs to{" "}
            <span className="font-mono">/fhir/&#123;Type&#125;</span>.
          </p>
        </div>
        <label className="text-xs text-slate-300">
          <span className="mr-2">Partner</span>
          <input
            type="text"
            value={partnerFilter}
            onChange={(e) => setPartnerFilter(e.target.value)}
            placeholder="all"
            className="w-40 rounded-md border border-slate-700 bg-slate-950 px-2 py-1 font-mono text-xs text-slate-100 focus:border-clinic-500 focus:outline-hidden"
          />
        </label>
      </header>

      {rows.isLoading && <div className="text-xs text-slate-400">Loading…</div>}

      {rows.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(rows.error)}
        </div>
      )}

      {rows.data && rows.data.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No inbound resources{partnerArg ? ` from ${partnerArg}` : ""}.
        </div>
      )}

      {rows.data && rows.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {rows.data.map((r) => (
            <InboundRow key={r.id} row={r} />
          ))}
        </ul>
      )}
    </section>
  );
};

const InboundRow = ({ row }: { row: InboundResourceDto }) => (
  <li className="grid grid-cols-12 items-center gap-2 py-2">
    <span className="col-span-2 truncate font-mono text-xs text-slate-300" title={row.partnerId}>
      {row.partnerId}
    </span>
    <span className="col-span-2 truncate text-xs text-slate-200">{row.resourceType}</span>
    <span className="col-span-4 truncate font-mono text-xs text-slate-400" title={row.logicalId}>
      {row.logicalId}
    </span>
    <span className="col-span-3 text-xs text-slate-400">{formatDateTime(row.receivedAtUtc)}</span>
    <span className="col-span-1">
      {row.validationOutcome ? (
        <span
          className="rounded-full border border-amber-700/60 bg-amber-950/30 px-2 py-0.5 text-xs text-amber-200"
          title={row.validationOutcome}
        >
          OK
        </span>
      ) : (
        <span className="text-xs text-slate-500">—</span>
      )}
    </span>
  </li>
);
