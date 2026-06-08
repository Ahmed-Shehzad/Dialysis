import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchDispatches, type AlarmDispatch } from "@/features/oncall/api/oncallApi";

/**
 * Alarm-dispatch audit page. Filters by date window + chair; expands each row to show
 * the per-attempt delivery history (chain link, channel, address, delivered y/n, failure
 * reason). The audit table fulfils the DPIA's "timely escalation" evidence requirement.
 */
export const OnCallAuditPage = () => {
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [chairId, setChairId] = useState("");
  const [expanded, setExpanded] = useState<string | null>(null);

  // Chair is filtered client-side: chairId is a Guid the user can't type, so we offer a picker
  // built from the chairs actually present in the date-windowed result set. Only from/to hit the
  // server (the date window can be large); the chair filter never reaches the wire.
  const query = useQuery({
    queryKey: ["pdms", "oncall", "dispatches", { from, to }],
    queryFn: () => fetchDispatches({ from: from || undefined, to: to || undefined }),
    refetchInterval: 30_000,
  });
  const allRows = query.data ?? [];
  const chairIds = [...new Set(allRows.map((r) => r.chairId))].sort((a, b) => a.localeCompare(b));
  const rows = chairId ? allRows.filter((r) => r.chairId === chairId) : allRows;

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">Alarm dispatch audit</h1>
        <p className="text-sm text-slate-400">
          Every IV-pump alarm raised, every page sent, every clinician acknowledgement.
        </p>
      </div>

      <div className="flex gap-2 text-sm">
        <label className="block">
          <span className="text-xs text-slate-400">From (UTC)</span>
          <input
            type="datetime-local"
            className="mt-1 rounded border border-slate-700 bg-slate-800/60 p-1.5 text-slate-100"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
          />
        </label>
        <label className="block">
          <span className="text-xs text-slate-400">To (UTC)</span>
          <input
            type="datetime-local"
            className="mt-1 rounded border border-slate-700 bg-slate-800/60 p-1.5 text-slate-100"
            value={to}
            onChange={(e) => setTo(e.target.value)}
          />
        </label>
        <label className="block">
          <span className="text-xs text-slate-400">Chair</span>
          <select
            className="mt-1 rounded border border-slate-700 bg-slate-800/60 p-1.5 text-slate-100 font-mono text-xs"
            value={chairId}
            onChange={(e) => setChairId(e.target.value)}
          >
            <option value="">All chairs</option>
            {chairIds.map((id) => (
              <option key={id} value={id}>
                {id.slice(0, 8)}
              </option>
            ))}
          </select>
        </label>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading dispatches…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load dispatches. Retry shortly.</div>
      )}
      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No alarm dispatches in the selected window.
        </div>
      )}
      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="w-40 pb-2 font-medium">Started (UTC)</th>
              <th className="w-24 pb-2 font-medium">Chair</th>
              <th className="pb-2 font-medium">Alarm</th>
              <th className="w-24 pb-2 font-medium">Severity</th>
              <th className="w-32 pb-2 font-medium">Status</th>
              <th className="w-20 pb-2 font-medium">Attempts</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <DispatchRow
                key={row.id}
                row={row}
                isExpanded={expanded === row.id}
                onToggle={() => setExpanded(expanded === row.id ? null : row.id)}
              />
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

const DispatchRow = ({
  row,
  isExpanded,
  onToggle,
}: {
  row: AlarmDispatch;
  isExpanded: boolean;
  onToggle: () => void;
}) => (
  <>
    <tr
      onClick={onToggle}
      className="cursor-pointer border-t border-slate-800/60 hover:bg-slate-800/30"
    >
      <td className="py-2 align-top font-mono text-xs">
        {new Date(row.startedAtUtc).toISOString().replace("T", " ").slice(0, 19)}
      </td>
      <td className="py-2 align-top font-mono text-xs">{row.chairId.slice(0, 8)}</td>
      <td className="py-2 align-top">{row.alarmCode}</td>
      <td className="py-2 align-top">{row.severity}</td>
      <td className="py-2 align-top">{row.status}</td>
      <td className="py-2 align-top">{row.attempts.length}</td>
    </tr>
    {isExpanded && (
      <tr className="border-t border-slate-800/40 bg-slate-900/40">
        <td colSpan={6} className="p-3">
          {row.attempts.length === 0 ? (
            <div className="text-xs text-slate-500">No attempts recorded.</div>
          ) : (
            <ul className="space-y-1 text-xs">
              {row.attempts.map((attempt, i) => (
                <li key={i} className="flex gap-3">
                  <span className="w-32 font-mono text-slate-400">
                    {new Date(attempt.attemptedAtUtc).toISOString().slice(11, 19)}
                  </span>
                  <span className="w-16">link {attempt.chainLinkIndex}</span>
                  <span className="w-24">{attempt.channel}</span>
                  <span className="flex-1 font-mono text-slate-400">{attempt.address}</span>
                  <span className={attempt.delivered ? "text-emerald-300" : "text-rose-300"}>
                    {attempt.delivered ? "delivered" : (attempt.failureReason ?? "failed")}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </td>
      </tr>
    )}
  </>
);

export default OnCallAuditPage;
