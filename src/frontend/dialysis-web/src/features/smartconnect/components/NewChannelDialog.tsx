import { useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { AdapterParametersForm } from "./AdapterParametersForm";
import { createFlow } from "../api/flows";
import { CHANNEL_TEMPLATES, type ChannelTemplateId, findTemplate } from "../api/channelTemplates";
import {
  FlowRuntimeState,
  type FlowRuntimeStateValue,
  type IntegrationFlow,
  type IntegrationFlowPipelineDefinition,
  type OutboundRouteSlot,
} from "../api/types";
import { humanizeError } from "@/lib/api/humanizeError";

type Props = {
  onClose: () => void;
};

// Adapter kinds registered in MutableFlowPluginRegistry. Keeping the list inline avoids a separate
// "/plugins" endpoint roundtrip for now — when registry introspection lands we can swap the source.
const ADAPTER_KINDS = [
  "transponder-bus",
  "http",
  "tcp",
  "file",
  "smtp",
  "database",
  "channel-writer",
  "pass-through",
] as const;

const newGuid = (): string => {
  // Browser-side GUID for the flow id — matches the backend's expectation that callers can supply
  // an id. crypto.randomUUID() is available in every browser we ship to.
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  // Fallback for older test runners.
  return "00000000-0000-4000-8000-" + Math.random().toString(16).slice(2, 14).padEnd(12, "0");
};

export const NewChannelDialog = ({ onClose }: Props) => {
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [templateId, setTemplateId] = useState<ChannelTemplateId>("hl7-mllp");
  const [startNow, setStartNow] = useState(false);
  const [pipeline, setPipeline] = useState<IntegrationFlowPipelineDefinition>(() =>
    findTemplate("hl7-mllp").build(),
  );

  const flowDraft = useMemo<IntegrationFlow>(
    () => ({
      id: newGuid(),
      name: name.trim(),
      runtimeState: (startNow
        ? FlowRuntimeState.Started
        : FlowRuntimeState.Stopped) as FlowRuntimeStateValue,
      pipeline,
      tags: [],
      groupId: null,
      description: description.trim() || null,
    }),
    [name, description, startNow, pipeline],
  );

  const create = useMutation({
    mutationFn: () => createFlow(flowDraft),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "flows"] });
      onClose();
    },
  });

  const onTemplateChange = (next: ChannelTemplateId) => {
    setTemplateId(next);
    setPipeline(findTemplate(next).build());
  };

  const onSubmit = () => {
    if (!name.trim()) return;
    create.mutate();
  };

  const onAddRoute = () => {
    setPipeline({
      ...pipeline,
      outboundRoutes: [
        ...pipeline.outboundRoutes,
        { ordinal: pipeline.outboundRoutes.length, kind: "pass-through", propertiesJson: null },
      ],
    });
  };

  const onRemoveRoute = (ordinal: number) => {
    setPipeline({
      ...pipeline,
      outboundRoutes: pipeline.outboundRoutes
        .filter((r) => r.ordinal !== ordinal)
        .map((r, i) => ({ ...r, ordinal: i })),
    });
  };

  const onUpdateRoute = (ordinal: number, patch: Partial<OutboundRouteSlot>) => {
    setPipeline({
      ...pipeline,
      outboundRoutes: pipeline.outboundRoutes.map((r) =>
        r.ordinal === ordinal ? { ...r, ...patch } : r,
      ),
    });
  };

  return (
    <div className="fixed inset-0 z-40 flex" role="dialog" aria-modal="true">
      <button
        type="button"
        aria-label="Close dialog"
        onClick={onClose}
        className="flex-1 bg-black/50"
      />
      <aside className="flex w-full max-w-3xl flex-col overflow-y-auto border-l border-slate-800 bg-slate-950 p-5 shadow-2xl">
        <header className="mb-4 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-clinic-100">New channel</h3>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
          >
            Close
          </button>
        </header>

        <div className="space-y-5 text-sm text-slate-200">
          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">1 · Basics</h4>
            <label className="block">
              <span className="text-xs text-slate-400">Name</span>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                maxLength={256}
                placeholder="e.g. HL7 v2 TCP Listener"
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
              />
            </label>
            <label className="block">
              <span className="text-xs text-slate-400">Description</span>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={2000}
                rows={2}
                placeholder="Optional — appears in the flows list and the operator dashboard."
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
              />
            </label>
          </section>

          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">2 · Template</h4>
            <div className="grid gap-2 sm:grid-cols-3">
              {CHANNEL_TEMPLATES.map((t) => (
                <button
                  key={t.id}
                  type="button"
                  onClick={() => onTemplateChange(t.id)}
                  className={`rounded-md border p-3 text-left text-xs transition ${
                    templateId === t.id
                      ? "border-clinic-500/60 bg-clinic-900/30"
                      : "border-slate-800 bg-slate-900/30 hover:border-slate-700"
                  }`}
                >
                  <div className="text-sm font-medium text-slate-100">{t.label}</div>
                  <div className="mt-1 text-[11px] text-slate-400">{t.description}</div>
                </button>
              ))}
            </div>
          </section>

          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">
              3 · Outbound routes ({pipeline.outboundRoutes.length})
            </h4>
            <label className="flex items-center gap-2 text-xs text-slate-300">
              <input
                type="checkbox"
                checked={pipeline.outboundRoutesSequential}
                onChange={(e) =>
                  setPipeline({ ...pipeline, outboundRoutesSequential: e.target.checked })
                }
              />
              Run routes sequentially (first failure stops later routes)
            </label>
            {pipeline.outboundRoutes.length === 0 && (
              <div className="rounded-md border border-slate-800 bg-slate-900/40 p-3 text-xs text-slate-500">
                No outbound routes. Add at least one for the channel to dispatch anywhere.
              </div>
            )}
            <div className="space-y-2">
              {pipeline.outboundRoutes.map((route) => (
                <div
                  key={route.ordinal}
                  className="rounded-md border border-slate-800 bg-slate-900/40 p-2"
                >
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-slate-500">#{route.ordinal}</span>
                    <select
                      value={route.kind}
                      onChange={(e) => onUpdateRoute(route.ordinal, { kind: e.target.value })}
                      className="flex-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
                    >
                      {ADAPTER_KINDS.map((k) => (
                        <option key={k} value={k}>
                          {k}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={() => onRemoveRoute(route.ordinal)}
                      className="rounded-md border border-rose-700 px-2 py-0.5 text-xs text-rose-300 hover:bg-rose-900/40"
                    >
                      Remove
                    </button>
                  </div>
                  <AdapterParametersForm
                    kind={route.kind}
                    propertiesJson={route.propertiesJson ?? null}
                    onChange={(next) => onUpdateRoute(route.ordinal, { propertiesJson: next })}
                  />
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={onAddRoute}
              className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800"
            >
              + Add route
            </button>
          </section>

          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">
              4 · Confirm and create
            </h4>
            <label className="flex items-center gap-2 text-xs text-slate-300">
              <input
                type="checkbox"
                checked={startNow}
                onChange={(e) => setStartNow(e.target.checked)}
              />
              Start the channel immediately after creation
            </label>
            <details>
              <summary className="cursor-pointer text-xs text-slate-400 hover:text-slate-200">
                Preview generated flow JSON
              </summary>
              <pre className="mt-2 max-h-72 overflow-auto rounded-md border border-slate-800 bg-slate-900/60 p-2 font-mono text-[10px] text-slate-300">
                {JSON.stringify(flowDraft, null, 2)}
              </pre>
            </details>

            {create.error && (
              <div className="rounded-md border border-rose-700/40 bg-rose-900/10 p-2 text-xs text-rose-200">
                {humanizeError(create.error)}
              </div>
            )}

            <div className="flex items-center justify-end gap-2 border-t border-slate-800 pt-3">
              <button
                type="button"
                onClick={onClose}
                className="rounded-md border border-slate-700 px-3 py-1 text-xs text-slate-300 hover:bg-slate-800"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={onSubmit}
                disabled={!name.trim() || create.isPending}
                className="rounded-md bg-clinic-600 px-3 py-1 text-xs font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
              >
                {create.isPending ? "Creating…" : "Create channel"}
              </button>
            </div>
          </section>
        </div>
      </aside>
    </div>
  );
};
