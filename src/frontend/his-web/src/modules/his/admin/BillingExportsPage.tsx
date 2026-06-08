import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { executeBillingExportJob, fetchBillingExportJobs } from "@/features/billing/api/billingApi";
import { humanizeError } from "@/lib/api/humanizeError";

const statusTone = (statusCode: string): string => {
  if (statusCode === "Completed") return "text-emerald-300";
  if (statusCode === "Failed") return "text-rose-300";
  return "text-amber-300";
};

/**
 * Operator dashboard for the HIS billing-export queue. Every job row is the trigger
 * HIS fires for EHR to assemble + submit claims for a payer/period window. Status
 * progresses Queued → Completed | Failed once the EHR consumer reports back. Operators
 * can (re-)dispatch a queued job to EHR with the Execute action.
 */
export const BillingExportsPage = () => {
  const [status, setStatus] = useState<string>("");
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["his", "billing", "exports", { status }],
    queryFn: () => fetchBillingExportJobs({ status: status || undefined, take: 200 }),
    refetchInterval: 30_000,
  });

  const execute = useMutation({
    mutationFn: (id: string) => executeBillingExportJob(id),
    onSettled: () =>
      void queryClient.invalidateQueries({ queryKey: ["his", "billing", "exports"] }),
  });

  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">Billing export jobs</h1>
          <p className="text-sm text-slate-400">
            Payer-billing windows queued for EHR to assemble + submit as EDI 837 claim batches.
          </p>
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-300">
          Status
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className="rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-slate-100"
          >
            <option value="">All</option>
            <option value="Queued">Queued</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
          </select>
        </label>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading export jobs…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load export jobs. Retry shortly.</div>
      )}

      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No export jobs match the current filter.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="w-44 pb-2 font-medium">Job id</th>
              <th className="w-24 pb-2 font-medium">Payer</th>
              <th className="w-28 pb-2 font-medium">Status</th>
              <th className="pb-2 font-medium">Period</th>
              <th className="pb-2 font-medium">Submitted</th>
              <th className="pb-2 font-medium">Completed</th>
              <th className="pb-2 font-medium">Notes</th>
              <th className="w-28 pb-2 font-medium">Action</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.id} className="border-t border-slate-800/60">
                <td className="py-2 align-top font-mono text-xs">{row.id.slice(0, 8)}…</td>
                <td className="py-2 align-top font-mono text-xs">{row.payerCode}</td>
                <td className="py-2 align-top">
                  <span className={statusTone(row.statusCode)}>{row.statusCode}</span>
                </td>
                <td className="py-2 align-top font-mono text-xs">
                  {row.periodStart} → {row.periodEnd}
                </td>
                <td className="py-2 align-top text-xs text-slate-400">
                  {new Date(row.submittedAtUtc).toISOString().slice(0, 19) + "Z"}
                </td>
                <td className="py-2 align-top text-xs text-slate-400">
                  {row.completedAtUtc
                    ? new Date(row.completedAtUtc).toISOString().slice(0, 19) + "Z"
                    : "—"}
                </td>
                <td className="py-2 align-top text-xs text-slate-400">{row.notes ?? "—"}</td>
                <td className="py-2 align-top">
                  {row.statusCode === "Queued" ? (
                    <button
                      type="button"
                      onClick={() => execute.mutate(row.id)}
                      disabled={execute.isPending && execute.variables === row.id}
                      className="rounded border border-emerald-700/70 bg-emerald-950/40 px-2 py-1 text-xs font-medium text-emerald-100 hover:bg-emerald-900/50 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {execute.isPending && execute.variables === row.id ? "Executing…" : "Execute"}
                    </button>
                  ) : (
                    <span className="text-xs text-slate-600">—</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {execute.error && (
        <p role="alert" className="text-xs text-rose-300">
          {humanizeError(execute.error)}
        </p>
      )}
    </div>
  );
};

export default BillingExportsPage;
