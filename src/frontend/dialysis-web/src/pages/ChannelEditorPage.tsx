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
import type {
  IntegrationFlow,
  IntegrationFlowPipelineDefinition,
} from "@/features/smartconnect/api/types";
import { PipelineGraph } from "@/features/smartconnect/components/PipelineGraph";
import { humanizeError } from "@/lib/api/humanizeError";

type EditorView = "graph" | "json";

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
  // Slice G2: graph view is the default for visual orientation; operators can flip to
  // raw-JSON for surgical edits the form-driven graph nodes don't surface yet.
  const [view, setView] = useState<EditorView>("graph");

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

      <div role="tablist" aria-label="Editor view" className="flex gap-2 text-xs">
        {(["graph", "json"] as const).map((mode) => (
          <button
            key={mode}
            role="tab"
            type="button"
            aria-selected={view === mode}
            onClick={() => setView(mode)}
            className={
              view === mode
                ? "rounded border border-slate-500 bg-slate-700 px-3 py-1 font-medium text-slate-100"
                : "rounded border border-slate-700 bg-transparent px-3 py-1 text-slate-300 hover:bg-slate-800"
            }
          >
            {mode === "graph" ? "Graph view" : "JSON view"}
          </button>
        ))}
      </div>

      {view === "graph" && (
        <section aria-labelledby="graph-view-heading" className="flex flex-col gap-2">
          <h2 id="graph-view-heading" className="text-sm font-semibold text-slate-200">
            Pipeline graph
          </h2>
          <p className="text-xs text-slate-400">
            Read-only visualisation of the source → filters → transforms → routes pipeline. Click
            the JSON view tab above for surgical edits. Saving from this page always writes the JSON
            the runtime consumes — the graph is an alternative input method, not a new wire format.
          </p>
          <ParsedPipelineGraph pipelineJson={pipelineJson} />
        </section>
      )}

      <section
        aria-labelledby="json-editor-heading"
        className={view === "json" ? "flex flex-col gap-2" : "hidden"}
      >
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

/**
 * Slice G2: parses the in-editor JSON into the typed pipeline shape and renders the
 * React Flow graph. Lives below ChannelEditorPage so the textarea state stays the single
 * source of truth — saving the channel always serialises from the textarea, never from a
 * separate parsed copy that could drift.
 */
function ParsedPipelineGraph({ pipelineJson }: { pipelineJson: string }): JSX.Element {
  let pipeline: IntegrationFlowPipelineDefinition | null = null;
  try {
    pipeline = JSON.parse(pipelineJson) as IntegrationFlowPipelineDefinition;
  } catch {
    pipeline = null;
  }
  if (pipeline === null) {
    return (
      <p className="text-xs text-amber-300">
        Pipeline JSON is currently invalid — switch to the JSON view to fix it before the graph can
        render.
      </p>
    );
  }
  return <PipelineGraph pipeline={pipeline} />;
}

export default ChannelEditorPage;
