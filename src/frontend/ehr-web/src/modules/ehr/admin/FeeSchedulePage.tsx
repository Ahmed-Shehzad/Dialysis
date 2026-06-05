import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchFeeSchedule,
  createFeeScheduleRow,
  reviseFeeScheduleRow,
  deleteFeeScheduleRow,
  type FeeScheduleRow,
} from "@/features/billing/api/feeScheduleApi";

/**
 * Operator management for the per-payer / per-CPT fee schedule that the charge consumer reads
 * on every administered service line. The resolution rule (exact payer beats wildcard `*`;
 * latest effective-from wins) is surfaced here so the operator can reason about which row a
 * given service will bill against. Add a row at onboarding, revise on every payer rate change.
 */
export const FeeSchedulePage = () => {
  const queryClient = useQueryClient();
  const [cptFilter, setCptFilter] = useState("");
  const [payerFilter, setPayerFilter] = useState("");
  const [editing, setEditing] = useState<FeeScheduleRow | null>(null);
  const [creating, setCreating] = useState(false);

  const query = useQuery({
    queryKey: ["ehr", "billing", "fee-schedule", { cptFilter, payerFilter }],
    queryFn: () =>
      fetchFeeSchedule({
        cptCode: cptFilter.trim() || undefined,
        payerCode: payerFilter.trim() || undefined,
      }),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["ehr", "billing", "fee-schedule"], exact: false });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteFeeScheduleRow(id),
    onSuccess: invalidate,
  });

  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">CPT fee schedule</h1>
          <p className="text-sm text-slate-400">
            Per-payer rates the charge consumer books against. Exact payer beats wildcard{" "}
            <span className="font-mono">*</span>; the latest effective date wins.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setCreating(true)}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500"
        >
          New rate
        </button>
      </div>

      <div className="flex items-center gap-3 text-xs text-slate-300">
        <label className="flex items-center gap-2">
          CPT
          <input
            value={cptFilter}
            onChange={(e) => setCptFilter(e.target.value)}
            placeholder="90935"
            className="w-28 rounded border border-slate-700 bg-slate-800/60 px-2 py-1 font-mono text-slate-100"
          />
        </label>
        <label className="flex items-center gap-2">
          Payer
          <input
            value={payerFilter}
            onChange={(e) => setPayerFilter(e.target.value)}
            placeholder="MED01 or *"
            className="w-28 rounded border border-slate-700 bg-slate-800/60 px-2 py-1 font-mono text-slate-100"
          />
        </label>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading fee schedule…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load the fee schedule. Retry shortly.</div>
      )}

      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No fee-schedule rows match. Add a rate to seed the table.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="w-24 pb-2 font-medium">CPT</th>
              <th className="w-28 pb-2 font-medium">Payer</th>
              <th className="w-32 pb-2 font-medium">Amount</th>
              <th className="pb-2 font-medium">Effective</th>
              <th className="w-32 pb-2 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.id} className="border-t border-slate-800/60">
                <td className="py-2 align-top font-mono text-xs">{row.cptCode}</td>
                <td className="py-2 align-top font-mono text-xs">{row.payerCode}</td>
                <td className="py-2 align-top">
                  {row.amount.toFixed(2)} {row.currencyCode}
                </td>
                <td className="py-2 align-top font-mono text-xs">
                  {row.effectiveFromUtc} → {row.effectiveUntilUtc ?? "open"}
                </td>
                <td className="py-2 align-top">
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => setEditing(row)}
                      className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteMutation.mutate(row.id)}
                      disabled={deleteMutation.isPending}
                      className="rounded border border-rose-700/50 px-2 py-1 text-xs text-rose-200 hover:border-rose-500 disabled:opacity-50"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {(creating || editing) && (
        <FeeScheduleDrawer
          row={editing}
          onClose={() => {
            setCreating(false);
            setEditing(null);
          }}
          onApplied={() => {
            invalidate();
            setCreating(false);
            setEditing(null);
          }}
        />
      )}
    </div>
  );
};

const FeeScheduleDrawer = ({
  row,
  onClose,
  onApplied,
}: {
  row: FeeScheduleRow | null;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const isEdit = row !== null;
  const [cptCode, setCptCode] = useState(row?.cptCode ?? "");
  const [payerCode, setPayerCode] = useState(row?.payerCode ?? "*");
  const [amount, setAmount] = useState(row ? String(row.amount) : "");
  const [currencyCode, setCurrencyCode] = useState(row?.currencyCode ?? "USD");
  const [effectiveFrom, setEffectiveFrom] = useState(
    row?.effectiveFromUtc ?? new Date().toISOString().slice(0, 10),
  );
  const [effectiveUntil, setEffectiveUntil] = useState(row?.effectiveUntilUtc ?? "");

  const mutation = useMutation({
    mutationFn: () => {
      const request = {
        cptCode: cptCode.trim(),
        payerCode: payerCode.trim(),
        amount: Number(amount),
        currencyCode: currencyCode.trim().toUpperCase(),
        effectiveFromUtc: effectiveFrom,
        effectiveUntilUtc: effectiveUntil || null,
      };
      return isEdit ? reviseFeeScheduleRow(row.id, request) : createFeeScheduleRow(request);
    },
    onSuccess: () => {
      onApplied();
      onClose();
    },
  });

  const canSubmit =
    cptCode.trim().length > 0 &&
    payerCode.trim().length > 0 &&
    currencyCode.trim().length === 3 &&
    Number(amount) >= 0 &&
    amount.trim().length > 0 &&
    !mutation.isPending;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-md border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-4 text-lg font-semibold text-slate-100">
          {isEdit ? "Revise rate" : "New rate"}
        </h2>

        <div className="grid grid-cols-2 gap-3">
          <label className="block text-sm">
            <span className="text-slate-400">CPT code</span>
            <input
              value={cptCode}
              onChange={(e) => setCptCode(e.target.value)}
              disabled={isEdit}
              placeholder="90935"
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 font-mono text-slate-100 disabled:opacity-60"
            />
          </label>
          <label className="block text-sm">
            <span className="text-slate-400">Payer code</span>
            <input
              value={payerCode}
              onChange={(e) => setPayerCode(e.target.value)}
              disabled={isEdit}
              placeholder="MED01 or *"
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 font-mono text-slate-100 disabled:opacity-60"
            />
          </label>
        </div>

        <div className="mt-3 grid grid-cols-2 gap-3">
          <label className="block text-sm">
            <span className="text-slate-400">Amount</span>
            <input
              type="number"
              step="0.01"
              min="0"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            />
          </label>
          <label className="block text-sm">
            <span className="text-slate-400">Currency (ISO-4217)</span>
            <input
              value={currencyCode}
              onChange={(e) => setCurrencyCode(e.target.value)}
              maxLength={3}
              placeholder="USD"
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 font-mono text-slate-100"
            />
          </label>
        </div>

        <div className="mt-3 grid grid-cols-2 gap-3">
          <label className="block text-sm">
            <span className="text-slate-400">Effective from</span>
            <input
              type="date"
              value={effectiveFrom}
              onChange={(e) => setEffectiveFrom(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            />
          </label>
          <label className="block text-sm">
            <span className="text-slate-400">Effective until (optional)</span>
            <input
              type="date"
              value={effectiveUntil}
              onChange={(e) => setEffectiveUntil(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            />
          </label>
        </div>

        {mutation.isError && (
          <div className="mt-3 text-xs text-rose-300">
            Save failed — check the effective window (until must be on or after from).
          </div>
        )}

        <div className="mt-5 flex justify-end gap-2 text-sm">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => mutation.mutate()}
            disabled={!canSubmit}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {mutation.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
};

export default FeeSchedulePage;
