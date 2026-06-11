import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchPolicies,
  replacePolicy,
  type EscalationPolicy,
} from "@/features/oncall/api/oncallApi";

/**
 * Escalation-policy editor. Typically one row per facility. Each row drives how long the
 * dispatcher waits at each chain link before walking forward, per alarm severity, with an
 * optional quiet-hours suppression flag for non-critical pages.
 */
export const OnCallPoliciesPage = () => {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["pdms", "oncall", "policies"],
    queryFn: () => fetchPolicies(),
  });
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["pdms", "oncall", "policies"] });
  const policy = query.data?.[0];

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">Escalation policy</h1>
        <p className="text-sm text-slate-400">
          Per-severity windows define how long the dispatcher waits before walking from primary →
          backup → supervisor. Quiet hours suppress non-critical pages between 22:00–06:00.
        </p>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading policies…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load the policy. Retry shortly.</div>
      )}

      {!query.isLoading && !policy && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No policy on file. The platform default (60s critical / 5m warning / 15m info) applies
          until one is configured.
        </div>
      )}

      {/* Keyed by policy id: a different policy remounts the editor with fresh local
          state, replacing the old "sync props into state in an effect" pattern. */}
      {policy && <PolicyEditor key={policy.id} policy={policy} onApplied={invalidate} />}
    </div>
  );
};

const PolicyEditor = ({
  policy,
  onApplied,
}: {
  policy: EscalationPolicy;
  onApplied: () => void;
}) => {
  const [name, setName] = useState(policy.name);
  const [criticalPrimary, setCriticalPrimary] = useState(policy.criticalPrimaryWindowSeconds);
  const [criticalBackup, setCriticalBackup] = useState(policy.criticalBackupWindowSeconds);
  const [warningPrimary, setWarningPrimary] = useState(policy.warningPrimaryWindowSeconds);
  const [warningBackup, setWarningBackup] = useState(policy.warningBackupWindowSeconds);
  const [informationalPrimary, setInformationalPrimary] = useState(
    policy.informationalPrimaryWindowSeconds,
  );
  const [quietHours, setQuietHours] = useState(policy.quietHoursSuppressNonCritical);
  // A different policy id remounts this editor via the `key` at the call site, so the
  // useState initializers above are the only props→state sync point (no sync effect).

  const mutation = useMutation({
    mutationFn: () =>
      replacePolicy(policy.id, {
        name,
        criticalPrimaryWindowSeconds: criticalPrimary,
        criticalBackupWindowSeconds: criticalBackup,
        warningPrimaryWindowSeconds: warningPrimary,
        warningBackupWindowSeconds: warningBackup,
        informationalPrimaryWindowSeconds: informationalPrimary,
        quietHoursSuppressNonCritical: quietHours,
      }),
    onSuccess: onApplied,
  });

  return (
    <div className="rounded border border-slate-800 bg-slate-900/40 p-4">
      <label className="mb-3 block text-sm">
        <span className="text-slate-400">Name</span>
        <input
          className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </label>

      <div className="grid grid-cols-2 gap-3 text-sm">
        <SecondsField
          label="Critical primary window (s)"
          value={criticalPrimary}
          onChange={setCriticalPrimary}
        />
        <SecondsField
          label="Critical backup window (s)"
          value={criticalBackup}
          onChange={setCriticalBackup}
        />
        <SecondsField
          label="Warning primary window (s)"
          value={warningPrimary}
          onChange={setWarningPrimary}
        />
        <SecondsField
          label="Warning backup window (s)"
          value={warningBackup}
          onChange={setWarningBackup}
        />
        <SecondsField
          label="Informational primary window (s)"
          value={informationalPrimary}
          onChange={setInformationalPrimary}
        />
      </div>

      <label className="mt-3 flex items-center gap-2 text-sm text-slate-300">
        <input
          type="checkbox"
          checked={quietHours}
          onChange={(e) => setQuietHours(e.target.checked)}
          className="accent-emerald-500"
        />
        Suppress non-critical pages between 22:00–06:00
      </label>

      {mutation.isError && (
        <div className="mt-3 text-xs text-rose-300">Save failed — retry shortly.</div>
      )}

      <div className="mt-4">
        <button
          type="button"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
        >
          {mutation.isPending ? "Saving…" : "Save"}
        </button>
      </div>
    </div>
  );
};

const SecondsField = ({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (n: number) => void;
}) => (
  <label className="block">
    <span className="text-slate-400">{label}</span>
    <input
      type="number"
      min={1}
      className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
      value={value}
      onChange={(e) => onChange(parseInt(e.target.value, 10) || 0)}
    />
  </label>
);

export default OnCallPoliciesPage;
