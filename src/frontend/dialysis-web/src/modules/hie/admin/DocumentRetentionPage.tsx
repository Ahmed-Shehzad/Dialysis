import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteRetentionPolicy,
  fetchRetentionPolicies,
  upsertRetentionPolicy,
  type RetentionPolicyRow,
} from "@/features/documents/api/retentionApi";

/**
 * Admin page for the per-kind retention windows on HIE Documents. The DPO maintains one row
 * per `DocumentReference.Kind` here; the platform's RetentionPurgerHostedService walks the
 * list every 24 h and soft-deletes + blob-purges documents past their window. No windows
 * are seeded on first launch — the page renders an empty state until the DPO upserts one.
 */
export const DocumentRetentionPage = () => {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<RetentionPolicyRow | "new" | null>(null);

  const query = useQuery({
    queryKey: ["hie", "documents", "retention"],
    queryFn: fetchRetentionPolicies,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["hie", "documents", "retention"], exact: false });

  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">Document retention</h1>
          <p className="text-sm text-slate-400">
            One window per document kind. The purger soft-deletes + blob-purges any document older
            than its window on a 24 h tick. No windows are seeded — set them here once the DPO has
            signed off.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setEditing("new")}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500"
        >
          New policy
        </button>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load policies. Retry shortly.</div>
      )}
      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No retention policies defined yet — the purger is a no-op until you add one.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="pb-2 font-medium">Kind</th>
              <th className="w-32 pb-2 font-medium">Retention</th>
              <th className="w-44 pb-2 font-medium">Updated</th>
              <th className="w-32 pb-2 font-medium">By</th>
              <th className="w-32 pb-2 font-medium" />
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.id} className="border-t border-slate-800/60">
                <td className="py-2 align-top font-medium text-slate-100">{row.kind}</td>
                <td className="py-2 align-top">{row.retentionDays} days</td>
                <td className="py-2 align-top font-mono text-xs">
                  {new Date(row.updatedAtUtc).toISOString().slice(0, 16).replace("T", " ")}
                </td>
                <td className="py-2 align-top text-xs">{row.updatedBy}</td>
                <td className="py-2 align-top text-right">
                  <button
                    type="button"
                    onClick={() => setEditing(row)}
                    className="mr-2 rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editing && (
        <PolicyDrawer
          row={editing === "new" ? null : editing}
          onClose={() => setEditing(null)}
          onApplied={() => {
            invalidate();
            setEditing(null);
          }}
        />
      )}
    </div>
  );
};

const PolicyDrawer = ({
  row,
  onClose,
  onApplied,
}: {
  row: RetentionPolicyRow | null;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const [kind, setKind] = useState(row?.kind ?? "");
  const [days, setDays] = useState(row?.retentionDays ?? 3650);

  const upsert = useMutation({
    mutationFn: () => upsertRetentionPolicy(kind, days),
    onSuccess: onApplied,
  });
  const remove = useMutation({
    mutationFn: () => deleteRetentionPolicy(kind),
    onSuccess: onApplied,
  });

  const canSave = kind.trim().length > 0 && days > 0 && !upsert.isPending;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-sm border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-3 text-lg font-semibold text-slate-100">
          {row ? `Revise ${row.kind}` : "New policy"}
        </h2>

        <label className="block text-sm">
          <span className="text-slate-400">Document kind</span>
          <input
            type="text"
            value={kind}
            disabled={row !== null}
            onChange={(e) => setKind(e.target.value)}
            placeholder="DischargeLetter"
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100 disabled:opacity-50"
          />
        </label>

        <label className="mt-3 block text-sm">
          <span className="text-slate-400">Retention (days)</span>
          <input
            type="number"
            value={days}
            min={1}
            onChange={(e) => setDays(parseInt(e.target.value, 10) || 0)}
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
          />
        </label>

        <p className="mt-3 text-xs text-slate-500">
          Common floors: clinical records → 3650 days (10 y); billing → 3650 days (HGB §257); admin
          uploads → 1825 days (5 y).
        </p>

        {(upsert.isError || remove.isError) && (
          <div className="mt-3 text-xs text-rose-300">Save failed — retry shortly.</div>
        )}

        <div className="mt-5 flex justify-between gap-2 text-sm">
          {row && (
            <button
              type="button"
              onClick={() => remove.mutate()}
              disabled={remove.isPending}
              className="rounded border border-rose-700 px-3 py-1.5 text-rose-200 hover:border-rose-500 disabled:opacity-50"
            >
              {remove.isPending ? "Removing…" : "Remove"}
            </button>
          )}
          <div className="ml-auto flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => upsert.mutate()}
              disabled={!canSave}
              className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
            >
              {upsert.isPending ? "Saving…" : "Save"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DocumentRetentionPage;
