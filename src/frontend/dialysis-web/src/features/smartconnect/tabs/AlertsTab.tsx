import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createAlertRule,
  deleteAlertRule,
  fetchAlertEvents,
  fetchAlertRules,
  testAlertRule,
  updateAlertRule,
} from "../api/alerts";
import {
  type AlertRule,
  AlertErrorType,
  AlertErrorTypeLabel,
  type AlertErrorTypeValue,
} from "../api/types";

const emptyRule = (): AlertRule => ({
  id: crypto.randomUUID(),
  name: "New alert rule",
  enabled: true,
  description: null,
  enabledFlowIds: null,
  errorPatterns: [{ errorType: AlertErrorType.OutboundFailure, regex: null }],
  actions: [],
  throttleWindow: null,
  revision: 0,
  lastModifiedUtc: new Date().toISOString(),
});

const RuleRow = ({ rule, onPick }: { rule: AlertRule; onPick: () => void }) => (
  <tr className="cursor-pointer hover:bg-slate-900/30" onClick={onPick}>
    <td className="px-3 py-2">
      <div className="text-slate-100">{rule.name}</div>
      {rule.description && <div className="text-xs text-slate-500">{rule.description}</div>}
    </td>
    <td className="px-3 py-2 text-xs">
      <span
        className={
          "rounded px-1.5 py-0.5 " +
          (rule.enabled ? "bg-emerald-700/30 text-emerald-200" : "bg-slate-700/30 text-slate-400")
        }
      >
        {rule.enabled ? "Enabled" : "Disabled"}
      </span>
    </td>
    <td className="px-3 py-2 text-xs text-slate-400">
      {rule.errorPatterns.map((p) => AlertErrorTypeLabel[p.errorType] ?? p.errorType).join(", ") || "—"}
    </td>
    <td className="px-3 py-2 text-xs text-slate-400">
      {rule.actions.length} action{rule.actions.length === 1 ? "" : "s"}
    </td>
    <td className="px-3 py-2 text-xs text-slate-400">{rule.throttleWindow ?? "—"}</td>
  </tr>
);

const RuleEditor = ({
  rule,
  onClose,
}: {
  rule: AlertRule;
  onClose: () => void;
}) => {
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<AlertRule>(rule);
  const dirty = JSON.stringify(draft) !== JSON.stringify(rule);

  const save = useMutation({
    mutationFn: () => updateAlertRule(draft),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "alert-rules"] });
      onClose();
    },
  });
  const remove = useMutation({
    mutationFn: () => deleteAlertRule(rule.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "alert-rules"] });
      onClose();
    },
  });
  const test = useMutation({
    mutationFn: () =>
      testAlertRule(rule.id, { errorType: AlertErrorType.OutboundFailure }, false),
  });

  return (
    <div className="fixed inset-0 z-40 flex" role="dialog" aria-modal="true">
      <button type="button" aria-label="Close" onClick={onClose} className="flex-1 bg-black/40" />
      <aside className="w-full max-w-xl space-y-3 overflow-y-auto border-l border-slate-800 bg-slate-950 p-5">
        <header className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-clinic-100">Alert rule</h3>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
          >
            Close
          </button>
        </header>

        <label className="block text-xs">
          <span className="text-slate-400">Name</span>
          <input
            value={draft.name}
            onChange={(e) => setDraft({ ...draft, name: e.target.value })}
            className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
          />
        </label>
        <label className="block text-xs">
          <span className="text-slate-400">Description</span>
          <textarea
            value={draft.description ?? ""}
            onChange={(e) => setDraft({ ...draft, description: e.target.value || null })}
            rows={2}
            className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-200"
          />
        </label>
        <label className="flex items-center gap-2 text-xs text-slate-300">
          <input
            type="checkbox"
            checked={draft.enabled}
            onChange={(e) => setDraft({ ...draft, enabled: e.target.checked })}
          />
          Enabled
        </label>

        <fieldset className="space-y-1 rounded-md border border-slate-800 p-3 text-xs">
          <legend className="px-1 text-slate-400">Error patterns</legend>
          {draft.errorPatterns.map((p, i) => (
            <div key={i} className="flex flex-wrap items-center gap-2">
              <select
                value={p.errorType}
                onChange={(e) => {
                  const next = [...draft.errorPatterns];
                  next[i] = { ...p, errorType: Number(e.target.value) as AlertErrorTypeValue };
                  setDraft({ ...draft, errorPatterns: next });
                }}
                className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-slate-200"
              >
                {Object.entries(AlertErrorTypeLabel).map(([v, label]) => (
                  <option key={v} value={v}>{label}</option>
                ))}
              </select>
              <input
                placeholder="regex (optional)"
                value={p.regex ?? ""}
                onChange={(e) => {
                  const next = [...draft.errorPatterns];
                  next[i] = { ...p, regex: e.target.value || null };
                  setDraft({ ...draft, errorPatterns: next });
                }}
                className="flex-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-slate-200 placeholder-slate-500"
              />
              <button
                type="button"
                onClick={() =>
                  setDraft({
                    ...draft,
                    errorPatterns: draft.errorPatterns.filter((_, j) => j !== i),
                  })
                }
                className="rounded-md border border-rose-700 px-2 py-0.5 text-rose-300 hover:bg-rose-900/40"
              >
                ✕
              </button>
            </div>
          ))}
          <button
            type="button"
            onClick={() =>
              setDraft({
                ...draft,
                errorPatterns: [
                  ...draft.errorPatterns,
                  { errorType: AlertErrorType.Any, regex: null },
                ],
              })
            }
            className="rounded-md border border-slate-700 px-2 py-0.5 text-slate-300 hover:bg-slate-800"
          >
            + Pattern
          </button>
        </fieldset>

        <label className="block text-xs">
          <span className="text-slate-400">Throttle window (TimeSpan, e.g. 00:05:00)</span>
          <input
            value={draft.throttleWindow ?? ""}
            onChange={(e) => setDraft({ ...draft, throttleWindow: e.target.value || null })}
            placeholder="00:05:00"
            className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-200"
          />
        </label>

        <fieldset className="space-y-1 rounded-md border border-slate-800 p-3 text-xs">
          <legend className="px-1 text-slate-400">Actions ({draft.actions.length})</legend>
          {draft.actions.length === 0 && (
            <div className="text-slate-500">
              No actions configured. Actions live in the database (kind + propertiesJson);
              v1 UI doesn't author them inline.
            </div>
          )}
          {draft.actions.map((a, i) => (
            <div key={i} className="rounded border border-slate-800 bg-slate-900/40 p-2">
              <div className="text-slate-200">{a.kind}</div>
              {a.propertiesJson && (
                <pre className="mt-1 max-h-24 overflow-auto text-xs text-slate-400">{a.propertiesJson}</pre>
              )}
            </div>
          ))}
        </fieldset>

        <div className="flex flex-wrap justify-end gap-2 border-t border-slate-800 pt-3">
          <button
            type="button"
            onClick={() => test.mutate()}
            disabled={test.isPending}
            className="rounded-md border border-slate-700 px-3 py-1 text-xs text-slate-200 hover:bg-slate-800 disabled:opacity-40"
          >
            {test.isPending ? "Testing…" : "Test fire"}
          </button>
          <button
            type="button"
            onClick={() => {
              if (confirm(`Delete rule "${rule.name}"?`)) remove.mutate();
            }}
            disabled={remove.isPending}
            className="rounded-md border border-rose-700 px-3 py-1 text-xs text-rose-300 hover:bg-rose-900/40 disabled:opacity-40"
          >
            Delete
          </button>
          <button
            type="button"
            onClick={() => save.mutate()}
            disabled={!dirty || save.isPending}
            className="rounded-md bg-clinic-600 px-3 py-1 text-xs font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
          >
            {save.isPending ? "Saving…" : "Save"}
          </button>
        </div>
        {test.data && (
          <div className="rounded-md border border-slate-800 bg-slate-900/40 p-2 text-xs text-slate-300">
            Test fired at {new Date(test.data.occurredAtUtc).toLocaleString()} — {test.data.actionOutcomes.length} action outcome{test.data.actionOutcomes.length === 1 ? "" : "s"}.
          </div>
        )}
      </aside>
    </div>
  );
};

export const AlertsTab = () => {
  const queryClient = useQueryClient();
  const rules = useQuery({
    queryKey: ["smartconnect", "alert-rules"],
    queryFn: () => fetchAlertRules(false),
  });
  const events = useQuery({
    queryKey: ["smartconnect", "alert-events"],
    queryFn: () => fetchAlertEvents({ take: 50 }),
    refetchInterval: 15_000,
  });
  const [editing, setEditing] = useState<AlertRule | null>(null);

  const create = useMutation({
    mutationFn: () => createAlertRule(emptyRule()),
    onSuccess: (rule) => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "alert-rules"] });
      setEditing(rule);
    },
  });

  return (
    <section className="space-y-6">
      <div>
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-medium text-slate-200">
            Rules {rules.data ? <span className="text-slate-500">({rules.data.length})</span> : null}
          </h3>
          <button
            type="button"
            onClick={() => create.mutate()}
            disabled={create.isPending}
            className="rounded-md bg-clinic-600 px-2 py-1 text-xs text-white hover:bg-clinic-700 disabled:opacity-40"
          >
            + New rule
          </button>
        </div>
        {rules.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
        {rules.error && <div className="text-xs text-rose-300">Alert service unavailable.</div>}
        {rules.data && rules.data.length === 0 && (
          <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
            No alert rules. Create one to start tracking error patterns.
          </div>
        )}
        {rules.data && rules.data.length > 0 && (
          <div className="overflow-hidden rounded-md border border-slate-800">
            <table className="w-full text-sm">
              <thead className="bg-slate-900/60 text-left text-xs uppercase text-slate-400">
                <tr>
                  <th className="px-3 py-2">Name</th>
                  <th className="px-3 py-2">State</th>
                  <th className="px-3 py-2">Patterns</th>
                  <th className="px-3 py-2">Actions</th>
                  <th className="px-3 py-2">Throttle</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {rules.data.map((r) => (
                  <RuleRow key={r.id} rule={r} onPick={() => setEditing(r)} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div>
        <h3 className="mb-2 text-sm font-medium text-slate-200">
          Recent alert events
          {events.data ? <span className="text-slate-500"> ({events.data.length})</span> : null}
        </h3>
        {events.data && events.data.length === 0 && (
          <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
            No alerts have fired recently.
          </div>
        )}
        {events.data && events.data.length > 0 && (
          <ul className="divide-y divide-slate-800 rounded-md border border-slate-800">
            {events.data.map((e) => (
              <li key={e.id} className="px-3 py-2 text-sm">
                <div className="flex justify-between text-xs text-slate-500">
                  <span>{new Date(e.occurredAtUtc).toLocaleString()}</span>
                  <span>{AlertErrorTypeLabel[e.errorType]}</span>
                </div>
                <div className="text-slate-200">{e.errorDetail ?? "(no detail)"}</div>
                <div className="text-xs text-slate-500">
                  rule={e.ruleId.slice(0, 8)} · flow={e.flowId?.slice(0, 8) ?? "—"} ·
                  outcomes={e.actionOutcomes.length}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {editing && <RuleEditor rule={editing} onClose={() => setEditing(null)} />}
    </section>
  );
};
