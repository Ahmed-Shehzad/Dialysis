// SmartConnect ↔ Mirth alignment — slice G2 (interactive). The Channel editor pairs the
// React Flow graph with a side drawer so operators add / edit / reorder / remove pipeline
// slots without dropping to raw JSON. The pipeline JSON view stays as a power-user escape
// hatch (collapsed by default); both views read from the same authoritative pipeline state
// so saving is unambiguous.

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router";
import { fetchFlow, updateFlow } from "@/features/smartconnect/api/flows";
import {
  CHANNEL_TEMPLATES,
  type ChannelTemplateId,
  findTemplate,
} from "@/features/smartconnect/api/channelTemplates";
import type {
  IntegrationFlow,
  IntegrationFlowPipelineDefinition,
} from "@/features/smartconnect/api/types";
import {
  PipelineGraph,
  type PipelineColumn,
} from "@/features/smartconnect/components/PipelineGraph";
import {
  PipelineNodeDrawer,
  type SelectedNode,
} from "@/features/smartconnect/components/PipelineNodeDrawer";
import { humanizeError } from "@/lib/api/humanizeError";

const DEFAULT_KIND_FOR_COLUMN: Record<PipelineColumn, string> = {
  filter: "allow-all",
  transform: "verify-hl7-strict",
  outbound: "pass-through",
};

const nodeIdToSelection = (id: string): SelectedNode | null => {
  const match = /^(filter|transform|outbound)-(\d+)$/.exec(id);
  if (!match) return null;
  return { column: match[1] as PipelineColumn, index: Number(match[2]) };
};

export function ChannelEditorPage(): JSX.Element {
  const { flowId } = useParams<{ flowId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const flowQuery = useQuery<IntegrationFlow>({
    queryKey: ["smartconnect", "flow", flowId],
    queryFn: () => fetchFlow(flowId!),
    enabled: Boolean(flowId),
  });

  // Operator edits live here; until the first edit the server response shows through
  // directly (derived below), replacing the old "seed state from the query in an effect"
  // pattern (react-hooks/set-state-in-effect). Once `edits` is non-null, background
  // refetches no longer clobber what the operator changed — same guarantee as before.
  const [edits, setEdits] = useState<{
    pipeline: IntegrationFlowPipelineDefinition | null;
    draft: string;
  } | null>(null);
  const [jsonParseError, setJsonParseError] = useState<string | null>(null);
  const [selection, setSelection] = useState<SelectedNode | null>(null);

  const serverPipeline = flowQuery.data?.pipeline ?? null;
  const pipeline = edits ? edits.pipeline : serverPipeline;
  const pipelineJsonDraft = edits
    ? edits.draft
    : serverPipeline
      ? JSON.stringify(serverPipeline, null, 2)
      : "";

  const applyPipeline = (next: IntegrationFlowPipelineDefinition) => {
    setEdits({ pipeline: next, draft: JSON.stringify(next, null, 2) });
    setJsonParseError(null);
  };

  const onAddNode = (column: PipelineColumn) => {
    if (!pipeline) return;
    const newSlot = {
      kind: DEFAULT_KIND_FOR_COLUMN[column],
      propertiesJson: null as string | null,
    };
    let next: IntegrationFlowPipelineDefinition;
    let insertedIndex: number;
    switch (column) {
      case "filter":
        insertedIndex = pipeline.routeFilters.length;
        next = { ...pipeline, routeFilters: [...pipeline.routeFilters, newSlot] };
        break;
      case "transform":
        insertedIndex = pipeline.sourceTransformStages.length;
        next = {
          ...pipeline,
          sourceTransformStages: [...pipeline.sourceTransformStages, newSlot],
        };
        break;
      case "outbound":
        insertedIndex = pipeline.outboundRoutes.length;
        next = {
          ...pipeline,
          outboundRoutes: [...pipeline.outboundRoutes, { ...newSlot, ordinal: insertedIndex }],
        };
        break;
    }
    applyPipeline(next);
    setSelection({ column, index: insertedIndex });
  };

  const applyTemplate = (templateId: ChannelTemplateId) => {
    applyPipeline(findTemplate(templateId).build());
    setSelection(null);
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!flowQuery.data || !pipeline) throw new Error("Flow not loaded yet.");
      const next: IntegrationFlow = { ...flowQuery.data, pipeline };
      await updateFlow(next);
      return next;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["smartconnect", "flow", flowId] });
      void queryClient.invalidateQueries({ queryKey: ["smartconnect", "flows"] });
    },
  });

  const jsonIsValid = useMemo(
    () => jsonParseError === null && pipeline !== null,
    [jsonParseError, pipeline],
  );

  if (!flowId) {
    return (
      <div className="p-6">
        <p className="text-sm text-amber-300">
          No flow selected. Navigate from <code>/integrations</code> → Flows tab → Edit.
        </p>
      </div>
    );
  }

  if (flowQuery.isLoading) {
    return <div className="p-6 text-sm text-slate-300">Loading channel definition…</div>;
  }

  if (flowQuery.isError) {
    return (
      <div className="p-6">
        <p className="text-sm text-rose-300">
          Could not load channel: {humanizeError(flowQuery.error)}
        </p>
      </div>
    );
  }

  const flow = flowQuery.data!;

  return (
    <div className="flex flex-col gap-4 p-6">
      <header className="flex items-baseline gap-3">
        <h1 className="text-xl font-semibold text-slate-100">Channel editor</h1>
        <code className="text-xs text-slate-400">{flow.name ?? flow.id}</code>
      </header>

      <div className="flex flex-wrap items-center gap-2 text-xs">
        <label className="flex items-center gap-2 text-slate-300">
          Replace from template:
          <select
            onChange={(e) => {
              const v = e.target.value;
              if (!v) return;
              applyTemplate(v as ChannelTemplateId);
              e.target.value = "";
            }}
            defaultValue=""
            className="rounded border border-slate-700 bg-slate-950 px-2 py-1 text-xs text-slate-100"
          >
            <option value="">— pick a template —</option>
            {CHANNEL_TEMPLATES.map((t) => (
              <option key={t.id} value={t.id}>
                {t.label}
              </option>
            ))}
          </select>
        </label>

        <span className="ml-auto" />

        <button
          type="button"
          disabled={!jsonIsValid || saveMutation.isPending}
          onClick={() => saveMutation.mutate()}
          className="rounded border border-slate-600 bg-clinic-600 px-3 py-1 text-xs font-medium text-white hover:bg-clinic-700 disabled:opacity-50"
        >
          {saveMutation.isPending ? "Saving…" : "Save channel"}
        </button>
        <button
          type="button"
          onClick={() => navigate("/integrations")}
          className="rounded border border-slate-700 bg-transparent px-3 py-1 text-xs text-slate-200 hover:bg-slate-800"
        >
          Back to flows
        </button>
      </div>

      {saveMutation.isError && (
        <p className="text-xs text-rose-300" role="alert">
          Save failed: {humanizeError(saveMutation.error)}
        </p>
      )}
      {saveMutation.isSuccess && (
        <p className="text-xs text-emerald-300">Channel definition saved.</p>
      )}

      <div className="flex gap-3">
        <section aria-labelledby="graph-view-heading" className="flex flex-1 flex-col gap-2">
          <h2 id="graph-view-heading" className="text-sm font-semibold text-slate-200">
            Pipeline
          </h2>
          <p className="text-xs text-slate-400">
            Click a node to edit it on the right. Click a <code>+ Add</code> placeholder to append a
            slot. Saving always writes the JSON the runtime consumes — the graph is the primary
            input method; the JSON view below is the escape hatch.
          </p>
          {pipeline ? (
            <PipelineGraph
              pipeline={pipeline}
              selectedNodeId={selection ? `${selection.column}-${selection.index}` : null}
              onSelectNode={(id) => setSelection(id ? nodeIdToSelection(id) : null)}
              onAddNode={onAddNode}
            />
          ) : (
            <p className="text-xs text-amber-300">
              Pipeline JSON is invalid — fix it in the JSON view below to re-render the graph.
            </p>
          )}
        </section>

        {pipeline && selection && (
          <PipelineNodeDrawer
            pipeline={pipeline}
            selection={selection}
            onChangePipeline={applyPipeline}
            onClose={() => setSelection(null)}
          />
        )}
      </div>

      <details className="rounded border border-slate-800 bg-slate-950/60 p-3">
        <summary className="cursor-pointer text-xs text-slate-300 hover:text-slate-100">
          Raw pipeline JSON (advanced)
        </summary>
        <p className="mt-2 text-[11px] text-slate-500">
          Edits here update the graph live. The save button is disabled while the JSON is invalid.
        </p>
        <textarea
          aria-label="Raw pipeline JSON"
          value={pipelineJsonDraft}
          onChange={(e) => {
            const value = e.target.value;
            try {
              const parsed = JSON.parse(value) as IntegrationFlowPipelineDefinition;
              setEdits({ pipeline: parsed, draft: value });
              setJsonParseError(null);
            } catch (err) {
              setEdits({ pipeline: null, draft: value });
              setJsonParseError((err as Error).message);
            }
          }}
          rows={18}
          spellCheck={false}
          className="mt-2 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-100"
        />
        {jsonParseError !== null && (
          <p className="mt-1 text-xs text-rose-300" role="alert">
            JSON parse error: {jsonParseError}
          </p>
        )}
      </details>
    </div>
  );
}

export default ChannelEditorPage;
