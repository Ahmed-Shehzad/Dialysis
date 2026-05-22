// Slice G of the SmartConnect ↔ Mirth alignment plan: scaffold for the visual channel
// editor. This first cut delivers raw-JSON pipeline editing (read fetchFlow → edit
// IntegrationFlowPipelineDefinition → save updateFlow) plus a documented placeholder
// for the React Flow drag-and-drop graph that lands in slice G2.
//
// Why JSON-first: getting the round-trip (load → edit → validate → save) wired through
// the existing TanStack Query mutation pattern is non-trivial on its own; the graph
// editor on top will benefit from a stable JSON path even after it ships, because
// channel JSON is the wire format we already import/export today (FlowsTab → import /
// export buttons). The graph editor is an alternative input method to the same JSON,
// not a replacement.

import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { fetchFlow, updateFlow } from "@/features/smartconnect/api/flows";
import type { IntegrationFlow } from "@/features/smartconnect/api/types";
import { humanizeError } from "@/lib/api/humanizeError";

export function ChannelEditorPage(): JSX.Element {
  const { flowId } = useParams<{ flowId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const flowQuery = useQuery<IntegrationFlow>({
    queryKey: ["smartconnect", "flow", flowId],
    queryFn: () => fetchFlow(flowId!),
    enabled: Boolean(flowId),
  });

  const [pipelineJson, setPipelineJson] = useState<string>("");
  const [parseError, setParseError] = useState<string | null>(null);

  // When the server response lands, seed the textarea — but keep operator edits when
  // the query refetches in the background (TanStack Query's default refetchOnFocus).
  useEffect(() => {
    if (flowQuery.data && pipelineJson === "") {
      setPipelineJson(JSON.stringify(flowQuery.data.pipeline, null, 2));
    }
  }, [flowQuery.data, pipelineJson]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!flowQuery.data) throw new Error("Flow not loaded yet.");
      const next: IntegrationFlow = {
        ...flowQuery.data,
        pipeline: JSON.parse(pipelineJson),
      };
      await updateFlow(next);
      return next;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["smartconnect", "flow", flowId] });
      void queryClient.invalidateQueries({ queryKey: ["smartconnect", "flows"] });
    },
  });

  const jsonIsValid = useMemo(() => {
    try {
      JSON.parse(pipelineJson);
      return true;
    } catch {
      return false;
    }
  }, [pipelineJson]);

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
    <div className="flex flex-col gap-6 p-6">
      <header className="flex items-baseline gap-3">
        <h1 className="text-xl font-semibold text-slate-100">Channel editor</h1>
        <code className="text-xs text-slate-400">{flow.name ?? flow.id}</code>
      </header>

      <section
        aria-labelledby="graph-editor-heading"
        className="rounded border border-dashed border-slate-700 bg-slate-900/40 p-4"
      >
        <h2 id="graph-editor-heading" className="text-sm font-semibold text-slate-200">
          Visual graph editor — planned (slice G2)
        </h2>
        <p className="mt-2 text-xs text-slate-400">
          The drag-and-drop graph view lands in a follow-up PR. Planned shape:
        </p>
        <ul className="mt-2 list-disc pl-5 text-xs text-slate-400">
          <li>
            React Flow (or <code>@xyflow/react</code>) for the source → filters → transforms →
            routes graph.
          </li>
          <li>
            Per-node forms driven by the per-plugin connector-property metadata from slice B (
            <code>ConnectorProperties</code>) so the form fields stay in sync with the runtime
            contract.
          </li>
          <li>
            Save still writes the same <code>IntegrationFlowPipelineDefinition</code> JSON below —
            the graph is an alternative input method, not a new wire format.
          </li>
        </ul>
      </section>

      <section aria-labelledby="json-editor-heading" className="flex flex-col gap-2">
        <h2 id="json-editor-heading" className="text-sm font-semibold text-slate-200">
          Pipeline JSON
        </h2>
        <p className="text-xs text-slate-400">
          Edit the pipeline definition as raw JSON; the graph editor will round-trip through this
          same field once it lands.
        </p>
        <textarea
          value={pipelineJson}
          onChange={(e) => {
            setPipelineJson(e.target.value);
            try {
              JSON.parse(e.target.value);
              setParseError(null);
            } catch (parseErr) {
              setParseError((parseErr as Error).message);
            }
          }}
          rows={24}
          spellCheck={false}
          className="w-full rounded border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-100"
        />
        {parseError !== null && (
          <p className="text-xs text-rose-300" role="alert">
            JSON parse error: {parseError}
          </p>
        )}
        {saveMutation.isError && (
          <p className="text-xs text-rose-300" role="alert">
            Save failed: {humanizeError(saveMutation.error)}
          </p>
        )}
        {saveMutation.isSuccess && (
          <p className="text-xs text-emerald-300">Channel definition saved.</p>
        )}
        <div className="flex gap-2">
          <button
            type="button"
            disabled={!jsonIsValid || saveMutation.isPending}
            onClick={() => saveMutation.mutate()}
            className="rounded border border-slate-600 bg-slate-800 px-3 py-1 text-xs font-medium text-slate-100 hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
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
      </section>
    </div>
  );
}

export default ChannelEditorPage;
