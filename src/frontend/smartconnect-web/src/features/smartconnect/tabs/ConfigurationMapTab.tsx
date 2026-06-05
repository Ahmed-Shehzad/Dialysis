import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteConfigMapEntry,
  fetchConfigMap,
  upsertConfigMapEntry,
  type ConfigMapEntry,
} from "../api/configMap";
import { fetchFlows } from "../api/flows";
import { VariableMapScope, type VariableMapScopeValue } from "../api/types";

const SCOPES: Array<{ key: VariableMapScopeValue; label: string; hint: string }> = [
  { key: VariableMapScope.Global, label: "Global", hint: "Server-wide variables" },
  {
    key: VariableMapScope.Configuration,
    label: "Configuration",
    hint: "Read-mostly key/value, host settings",
  },
  { key: VariableMapScope.GlobalChannel, label: "GlobalChannel", hint: "All channels see these" },
  {
    key: VariableMapScope.Channel,
    label: "Channel",
    hint: "Per-flow overrides (requires flow id)",
  },
];

export const ConfigurationMapTab = () => {
  const [scope, setScope] = useState<VariableMapScopeValue>(VariableMapScope.Global);
  const [flowId, setFlowId] = useState<string>("");
  const [newKey, setNewKey] = useState("");
  const [newValue, setNewValue] = useState("");
  const queryClient = useQueryClient();

  const flows = useQuery({ queryKey: ["smartconnect", "flows"], queryFn: fetchFlows });
  const channelScope = scope === VariableMapScope.Channel;
  const entries = useQuery({
    queryKey: ["smartconnect", "config-map", scope, flowId],
    queryFn: () => fetchConfigMap(scope, channelScope ? flowId || undefined : undefined),
    enabled: !channelScope || Boolean(flowId),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["smartconnect", "config-map", scope, flowId] });

  const upsert = useMutation({
    mutationFn: (entry: ConfigMapEntry) =>
      upsertConfigMapEntry(scope, entry, channelScope ? flowId || undefined : undefined),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (key: string) =>
      deleteConfigMapEntry(scope, key, channelScope ? flowId || undefined : undefined),
    onSuccess: invalidate,
  });

  const scopeHint = useMemo(() => SCOPES.find((s) => s.key === scope)?.hint ?? "", [scope]);

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-end gap-2 text-xs">
        <div>
          <label className="block text-slate-400">Scope</label>
          <select
            value={scope}
            onChange={(e) => setScope(e.target.value as VariableMapScopeValue)}
            className="mt-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-slate-200"
          >
            {SCOPES.map((s) => (
              <option key={s.key} value={s.key}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
        {channelScope && (
          <div>
            <label className="block text-slate-400">Flow</label>
            <select
              value={flowId}
              onChange={(e) => setFlowId(e.target.value)}
              className="mt-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-slate-200"
            >
              <option value="">Pick a flow…</option>
              {flows.data?.map((f) => (
                <option key={f.id} value={f.id}>
                  {f.name}
                </option>
              ))}
            </select>
          </div>
        )}
        <span className="text-slate-500">{scopeHint}</span>
      </div>

      {channelScope && !flowId && (
        <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
          Pick a flow to view its channel-scoped variables.
        </div>
      )}

      {entries.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
      {entries.error && <div className="text-xs text-rose-300">Could not load config map.</div>}
      {entries.data && (
        <div className="overflow-hidden rounded-md border border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-900/60 text-left text-xs uppercase text-slate-400">
              <tr>
                <th className="w-1/3 px-3 py-2">Key</th>
                <th className="px-3 py-2">Value</th>
                <th className="px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {entries.data.map((e) => (
                <ConfigRow
                  key={e.key}
                  entry={e}
                  onSave={(v) => upsert.mutate({ key: e.key, value: v })}
                  onDelete={() => remove.mutate(e.key)}
                  saving={upsert.isPending}
                  deleting={remove.isPending}
                />
              ))}
              {entries.data.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-3 py-4 text-center text-xs text-slate-500">
                    No entries in this scope yet.
                  </td>
                </tr>
              )}
            </tbody>
            <tfoot className="bg-slate-900/40">
              <tr>
                <td className="px-3 py-2">
                  <input
                    placeholder="new.key"
                    value={newKey}
                    onChange={(e) => setNewKey(e.target.value)}
                    className="w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
                  />
                </td>
                <td className="px-3 py-2">
                  <input
                    placeholder="value"
                    value={newValue}
                    onChange={(e) => setNewValue(e.target.value)}
                    className="w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
                  />
                </td>
                <td className="px-3 py-2 text-right">
                  <button
                    type="button"
                    disabled={!newKey || upsert.isPending}
                    onClick={() => {
                      upsert.mutate({ key: newKey, value: newValue });
                      setNewKey("");
                      setNewValue("");
                    }}
                    className="rounded-md bg-clinic-600 px-2 py-0.5 text-xs text-white hover:bg-clinic-700 disabled:opacity-40"
                  >
                    Add
                  </button>
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </section>
  );
};

const ConfigRow = ({
  entry,
  onSave,
  onDelete,
  saving,
  deleting,
}: {
  entry: ConfigMapEntry;
  onSave: (value: string) => void;
  onDelete: () => void;
  saving: boolean;
  deleting: boolean;
}) => {
  const [draft, setDraft] = useState(entry.value);
  const dirty = draft !== entry.value;
  return (
    <tr>
      <td className="px-3 py-2 font-mono text-xs text-slate-200">{entry.key}</td>
      <td className="px-3 py-2">
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          className="w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
        />
      </td>
      <td className="px-3 py-2 text-right">
        <div className="flex justify-end gap-1.5">
          <button
            type="button"
            disabled={!dirty || saving}
            onClick={() => onSave(draft)}
            className="rounded-md bg-emerald-600/80 px-2 py-0.5 text-xs text-white hover:bg-emerald-700 disabled:opacity-40"
          >
            Save
          </button>
          <button
            type="button"
            disabled={deleting}
            onClick={() => {
              if (confirm(`Delete ${entry.key}?`)) onDelete();
            }}
            className="rounded-md border border-rose-700 px-2 py-0.5 text-xs text-rose-300 hover:bg-rose-900/40 disabled:opacity-40"
          >
            Delete
          </button>
        </div>
      </td>
    </tr>
  );
};
