import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AdapterParametersForm } from "./AdapterParametersForm";
import { createFlow, fetchFlows } from "../api/flows";
import { CHANNEL_TEMPLATES, type ChannelTemplateId, findTemplate } from "../api/channelTemplates";
import {
  CHANNEL_DATA_TYPES,
  type ChannelAttachmentReference,
  type ChannelDataType,
  FlowRuntimeState,
  type FlowRuntimeStateValue,
  type IntegrationFlow,
  type IntegrationFlowPipelineDefinition,
  type OutboundRouteSlot,
  type RouteFilterSlot,
  type TransformStageSlot,
} from "../api/types";
import { getRouteFilterSchema, getTransformStageSchema } from "../api/adapterSchemas";
import { humanizeError } from "@/lib/api/humanizeError";

type Props = {
  onClose: () => void;
};

const MAX_ATTACHMENT_BYTES = 1 * 1024 * 1024;

const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]!);
  return btoa(binary);
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

const ROUTE_FILTER_KINDS = [
  "allow-all",
  "verify-hl7",
  "verify-fhir",
  "javascript",
  "rule-builder",
  "iterator",
  "external-script",
] as const;

const TRANSFORM_STAGE_KINDS = [
  "verify-hl7-strict",
  "verify-fhir-strict",
  "hl7-to-fhir-pipeline",
  "javascript",
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
  // HL7-MLLP / HL7-File templates seed dataTypes=["HL7v2"]; Custom starts empty.
  const [dataTypes, setDataTypes] = useState<ChannelDataType[]>(["HL7v2"]);
  const [tagsRaw, setTagsRaw] = useState("");
  const [dependencies, setDependencies] = useState<string[]>([]);
  const [attachments, setAttachments] = useState<ChannelAttachmentReference[]>([]);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);

  // Lookup for the dependency picker. We display "name (state)" per option so the operator can
  // see at a glance whether starting will succeed.
  const flows = useQuery({
    queryKey: ["smartconnect", "flows"],
    queryFn: fetchFlows,
  });

  const tags = useMemo(
    () =>
      tagsRaw
        .split(/[,\s]+/)
        .map((t) => t.trim())
        .filter(Boolean),
    [tagsRaw],
  );

  const flowDraft = useMemo<IntegrationFlow>(
    () => ({
      id: newGuid(),
      name: name.trim(),
      runtimeState: (startNow
        ? FlowRuntimeState.Started
        : FlowRuntimeState.Stopped) as FlowRuntimeStateValue,
      pipeline,
      tags,
      groupId: null,
      description: description.trim() || null,
      dataTypes,
      dependencies,
      attachments,
    }),
    [name, description, startNow, pipeline, tags, dataTypes, dependencies, attachments],
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
    // HL7 templates seed dataTypes=["HL7v2"]; Custom clears (operator picks).
    if (next === "hl7-mllp" || next === "hl7-file") {
      setDataTypes(["HL7v2"]);
    } else {
      setDataTypes([]);
    }
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

  const onAddFilter = () => {
    setPipeline({
      ...pipeline,
      routeFilters: [...pipeline.routeFilters, { kind: "allow-all", propertiesJson: null }],
    });
  };

  const onRemoveFilter = (index: number) => {
    setPipeline({
      ...pipeline,
      routeFilters: pipeline.routeFilters.filter((_, i) => i !== index),
    });
  };

  const onUpdateFilter = (index: number, patch: Partial<RouteFilterSlot>) => {
    setPipeline({
      ...pipeline,
      routeFilters: pipeline.routeFilters.map((f, i) => (i === index ? { ...f, ...patch } : f)),
    });
  };

  const onAddStage = () => {
    setPipeline({
      ...pipeline,
      sourceTransformStages: [
        ...pipeline.sourceTransformStages,
        { kind: "verify-hl7-strict", propertiesJson: null },
      ],
    });
  };

  const onRemoveStage = (index: number) => {
    setPipeline({
      ...pipeline,
      sourceTransformStages: pipeline.sourceTransformStages.filter((_, i) => i !== index),
    });
  };

  const onUpdateStage = (index: number, patch: Partial<TransformStageSlot>) => {
    setPipeline({
      ...pipeline,
      sourceTransformStages: pipeline.sourceTransformStages.map((s, i) =>
        i === index ? { ...s, ...patch } : s,
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

          <section className="space-y-3">
            <h4 className="text-xs font-semibold uppercase text-slate-400">3 · Metadata</h4>

            <div>
              <span className="text-xs text-slate-400">Data types</span>
              <div className="mt-1 flex flex-wrap gap-1">
                {CHANNEL_DATA_TYPES.map((dt) => {
                  const selected = dataTypes.includes(dt);
                  return (
                    <button
                      key={dt}
                      type="button"
                      onClick={() =>
                        setDataTypes(
                          selected ? dataTypes.filter((x) => x !== dt) : [...dataTypes, dt],
                        )
                      }
                      className={`rounded-full border px-2 py-0.5 text-[11px] transition ${
                        selected
                          ? "border-clinic-500/60 bg-clinic-900/30 text-clinic-100"
                          : "border-slate-700 bg-slate-900/30 text-slate-400 hover:border-slate-500"
                      }`}
                    >
                      {dt}
                    </button>
                  );
                })}
              </div>
            </div>

            <label className="block">
              <span className="text-xs text-slate-400">Tags (comma or space separated)</span>
              <input
                type="text"
                value={tagsRaw}
                onChange={(e) => setTagsRaw(e.target.value)}
                placeholder="hl7 production north-hospital"
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
              />
              {tags.length > 0 && (
                <div className="mt-1 flex flex-wrap gap-1">
                  {tags.map((t) => (
                    <span
                      key={t}
                      className="rounded bg-slate-800/70 px-1.5 py-0.5 text-[10px] text-slate-400"
                    >
                      {t}
                    </span>
                  ))}
                </div>
              )}
            </label>

            <div>
              <span className="text-xs text-slate-400">
                Depends on (Start refuses unless every selected dependency is Started)
              </span>
              {flows.isLoading && (
                <div className="mt-1 text-[11px] text-slate-500">Loading flow list…</div>
              )}
              {flows.data && flows.data.length === 0 && (
                <div className="mt-1 text-[11px] text-slate-500">No other flows exist yet.</div>
              )}
              {flows.data && flows.data.length > 0 && (
                <div className="mt-1 max-h-32 overflow-y-auto rounded-md border border-slate-800 bg-slate-900/40 p-1">
                  {flows.data.map((f) => {
                    const selected = dependencies.includes(f.id);
                    return (
                      <label
                        key={f.id}
                        className="flex cursor-pointer items-center gap-2 px-2 py-1 text-xs text-slate-200 hover:bg-slate-800"
                      >
                        <input
                          type="checkbox"
                          checked={selected}
                          onChange={(e) =>
                            setDependencies(
                              e.target.checked
                                ? [...dependencies, f.id]
                                : dependencies.filter((x) => x !== f.id),
                            )
                          }
                        />
                        <span className="grow">{f.name}</span>
                        <span className="text-[10px] text-slate-500">
                          {f.runtimeState === FlowRuntimeState.Started ? "Started" : "Stopped"}
                        </span>
                      </label>
                    );
                  })}
                </div>
              )}
            </div>

            <div>
              <span className="text-xs text-slate-400">
                Attachments (reference docs &mdash; sample messages, profiles, vendor docs &mdash; ≤
                1 MiB each)
              </span>
              <input
                type="file"
                aria-label="Upload channel attachment"
                title="Upload channel attachment"
                onChange={async (e) => {
                  const file = e.target.files?.[0];
                  if (!file) return;
                  setAttachmentError(null);
                  if (file.size > MAX_ATTACHMENT_BYTES) {
                    setAttachmentError(
                      `'${file.name}' is ${(file.size / 1024).toFixed(0)} KiB — over the 1 MiB cap.`,
                    );
                    e.target.value = "";
                    return;
                  }
                  const buf = await file.arrayBuffer();
                  setAttachments([
                    ...attachments,
                    {
                      name: file.name,
                      mimeType: file.type || "application/octet-stream",
                      base64Bytes: arrayBufferToBase64(buf),
                      description: null,
                    },
                  ]);
                  e.target.value = "";
                }}
                className="mt-1 w-full text-xs text-slate-300"
              />
              {attachmentError && (
                <div className="mt-1 text-[11px] text-rose-300">{attachmentError}</div>
              )}
              {attachments.length > 0 && (
                <div className="mt-2 space-y-1">
                  {attachments.map((a, i) => (
                    <div
                      key={`${a.name}-${i}`}
                      className="flex items-center justify-between rounded-md border border-slate-800 bg-slate-900/40 px-2 py-1 text-xs text-slate-200"
                    >
                      <div>
                        <div>{a.name}</div>
                        <div className="text-[10px] text-slate-500">
                          {a.mimeType} · {Math.ceil((a.base64Bytes.length * 3) / 4 / 1024)} KiB
                        </div>
                      </div>
                      <button
                        type="button"
                        onClick={() => setAttachments(attachments.filter((_, j) => j !== i))}
                        className="rounded border border-rose-700 px-1.5 py-0.5 text-[10px] text-rose-300 hover:bg-rose-900/40"
                      >
                        Remove
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>

          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">
              3.5 · Route filters ({pipeline.routeFilters.length})
            </h4>
            <p className="text-[11px] text-slate-500">
              Filters run on every inbound message before the source transforms. Drop here, fail
              later — pair <code>verify-hl7</code> with the strict transform stage if you want loud
              failures instead of silent drops.
            </p>
            {pipeline.routeFilters.length === 0 && (
              <div className="rounded-md border border-slate-800 bg-slate-900/40 p-3 text-xs text-slate-500">
                No route filters. Messages pass through unconditionally.
              </div>
            )}
            <div className="space-y-2">
              {pipeline.routeFilters.map((filter, i) => (
                <div key={i} className="rounded-md border border-slate-800 bg-slate-900/40 p-2">
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-slate-500">#{i}</span>
                    <select
                      aria-label={`Route filter #${i} kind`}
                      value={filter.kind}
                      onChange={(e) => onUpdateFilter(i, { kind: e.target.value })}
                      className="flex-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
                    >
                      {ROUTE_FILTER_KINDS.map((k) => (
                        <option key={k} value={k}>
                          {k}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={() => onRemoveFilter(i)}
                      className="rounded-md border border-rose-700 px-2 py-0.5 text-xs text-rose-300 hover:bg-rose-900/40"
                    >
                      Remove
                    </button>
                  </div>
                  <AdapterParametersForm
                    kind={filter.kind}
                    propertiesJson={filter.propertiesJson ?? null}
                    onChange={(next) => onUpdateFilter(i, { propertiesJson: next })}
                    schemaResolver={getRouteFilterSchema}
                  />
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={onAddFilter}
              className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800"
            >
              + Add filter
            </button>
          </section>

          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">
              3.7 · Source transform stages ({pipeline.sourceTransformStages.length})
            </h4>
            <p className="text-[11px] text-slate-500">
              Stages run on every inbound message after the filters but before the outbound routes.
              Use <code>hl7-to-fhir-pipeline</code> to convert HL7 v2 to FHIR R4, then{" "}
              <code>verify-fhir-strict</code> to fail loudly on an invalid Bundle.
            </p>
            {pipeline.sourceTransformStages.length === 0 && (
              <div className="rounded-md border border-slate-800 bg-slate-900/40 p-3 text-xs text-slate-500">
                No source transform stages. Inbound payloads pass through unchanged.
              </div>
            )}
            <div className="space-y-2">
              {pipeline.sourceTransformStages.map((stage, i) => (
                <div key={i} className="rounded-md border border-slate-800 bg-slate-900/40 p-2">
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-slate-500">#{i}</span>
                    <select
                      aria-label={`Source transform stage #${i} kind`}
                      value={stage.kind}
                      onChange={(e) => onUpdateStage(i, { kind: e.target.value })}
                      className="flex-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
                    >
                      {TRANSFORM_STAGE_KINDS.map((k) => (
                        <option key={k} value={k}>
                          {k}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={() => onRemoveStage(i)}
                      className="rounded-md border border-rose-700 px-2 py-0.5 text-xs text-rose-300 hover:bg-rose-900/40"
                    >
                      Remove
                    </button>
                  </div>
                  <AdapterParametersForm
                    kind={stage.kind}
                    propertiesJson={stage.propertiesJson ?? null}
                    onChange={(next) => onUpdateStage(i, { propertiesJson: next })}
                    schemaResolver={getTransformStageSchema}
                  />
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={onAddStage}
              className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800"
            >
              + Add stage
            </button>
          </section>

          <section className="space-y-2">
            <h4 className="text-xs font-semibold uppercase text-slate-400">
              4 · Outbound routes ({pipeline.outboundRoutes.length})
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
                      aria-label={`Outbound route #${route.ordinal} kind`}
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
              5 · Confirm and create
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
