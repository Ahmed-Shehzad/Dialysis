import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchInventory,
  receiveStock,
  adjustStock,
  type InventoryItem,
} from "@/features/inventory/api/inventoryApi";

/**
 * Operator dashboard for the medication-inventory ledger. Lists every item with a
 * Low-stock badge when on-hand falls at or below threshold, and surfaces the
 * Receive / Adjust drawer for the operator to record physical-count reconciliation
 * or stock arrivals.
 */
export const InventoryPage = () => {
  const queryClient = useQueryClient();
  const [lowOnly, setLowOnly] = useState(false);
  const [selected, setSelected] = useState<InventoryItem | null>(null);

  const query = useQuery({
    queryKey: ["pdms", "inventory", { lowOnly }],
    queryFn: () => fetchInventory(lowOnly),
    refetchInterval: 60_000,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["pdms", "inventory"], exact: false });

  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">Medication inventory</h1>
          <p className="text-sm text-slate-400">
            Pharmacy stock — per medication + lot. Deduction is automatic on every MAR write.
          </p>
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-300">
          <input
            type="checkbox"
            checked={lowOnly}
            onChange={(e) => setLowOnly(e.target.checked)}
            className="accent-emerald-500"
          />
          Low-stock only
        </label>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading inventory…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load inventory. Retry shortly.</div>
      )}

      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No inventory rows match. Receive new stock to populate the ledger.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="pb-2 font-medium">Medication</th>
              <th className="w-32 pb-2 font-medium">Lot</th>
              <th className="w-32 pb-2 font-medium">Expiry</th>
              <th className="w-20 pb-2 font-medium">On hand</th>
              <th className="w-20 pb-2 font-medium">Threshold</th>
              <th className="w-32 pb-2 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.id} className="border-t border-slate-800/60">
                <td className="py-2 align-top">
                  <div>{row.medicationDisplay}</div>
                  <div className="text-xs text-slate-500">
                    {row.medicationCodeSystem.split("/").pop()}:{row.medicationCode}
                  </div>
                </td>
                <td className="py-2 align-top font-mono text-xs">{row.lotNumber}</td>
                <td className="py-2 align-top font-mono text-xs">
                  {new Date(row.expiryUtc).toISOString().slice(0, 10)}
                </td>
                <td className="py-2 align-top">
                  {row.onHandUnits}
                  {row.lowStock && (
                    <span className="ml-2 rounded bg-amber-900/40 px-1.5 py-0.5 text-xs text-amber-200">
                      Low
                    </span>
                  )}
                </td>
                <td className="py-2 align-top">{row.threshold}</td>
                <td className="py-2 align-top">
                  <button
                    type="button"
                    onClick={() => setSelected(row)}
                    className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                  >
                    Receive / Adjust
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {selected && (
        <InventoryActionDrawer
          item={selected}
          onClose={() => setSelected(null)}
          onApplied={invalidate}
        />
      )}
    </div>
  );
};

const InventoryActionDrawer = ({
  item,
  onClose,
  onApplied,
}: {
  item: InventoryItem;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const [units, setUnits] = useState("");
  const [reason, setReason] = useState("");
  const [mode, setMode] = useState<"receive" | "adjust">("receive");

  const mutation = useMutation({
    mutationFn: () =>
      mode === "receive"
        ? receiveStock(item.id, parseInt(units, 10), reason)
        : adjustStock(item.id, parseInt(units, 10), reason),
    onSuccess: () => {
      onApplied();
      onClose();
    },
  });

  const canSubmit = parseInt(units, 10) > 0 && reason.trim().length > 0 && !mutation.isPending;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-sm border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-3 text-lg font-semibold text-slate-100">{item.medicationDisplay}</h2>
        <div className="mb-4 text-xs text-slate-400">
          Lot {item.lotNumber} · on hand {item.onHandUnits}
        </div>

        <div className="mb-3 flex gap-1 border-b border-slate-800">
          <button
            type="button"
            onClick={() => setMode("receive")}
            className={
              "px-3 py-1.5 text-sm " +
              (mode === "receive"
                ? "border-b-2 border-emerald-400 text-slate-100"
                : "text-slate-400")
            }
          >
            Receive
          </button>
          <button
            type="button"
            onClick={() => setMode("adjust")}
            className={
              "px-3 py-1.5 text-sm " +
              (mode === "adjust"
                ? "border-b-2 border-emerald-400 text-slate-100"
                : "text-slate-400")
            }
          >
            Adjust to count
          </button>
        </div>

        <label className="block text-sm">
          <span className="text-slate-400">
            {mode === "receive" ? "Units received" : "New on-hand"}
          </span>
          <input
            type="number"
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            value={units}
            onChange={(e) => setUnits(e.target.value)}
          />
        </label>

        <label className="mt-3 block text-sm">
          <span className="text-slate-400">Reason</span>
          <textarea
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={
              mode === "receive"
                ? "Vendor delivery PO-12345"
                : "Quarterly physical count — 3 expired vials discarded"
            }
          />
        </label>

        {mutation.isError && (
          <div className="mt-3 text-xs text-rose-300">Save failed — retry shortly.</div>
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

export default InventoryPage;
