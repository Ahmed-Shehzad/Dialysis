// SmartConnect ↔ Mirth alignment — slice G2 (interactive). React Flow rendering of an
// IntegrationFlowPipelineDefinition. Read-only when `onSelectNode` is omitted (the original
// G2 visualisation); becomes the editor surface when the caller passes selection + add-node
// callbacks (paired with PipelineNodeDrawer for kind / parameters editing).
//
// Edits round-trip through the JSON the runtime consumes — the graph is an alternative
// input method, not a new wire format.

import { useCallback, useMemo } from "react";
import {
  Background,
  Controls,
  MarkerType,
  ReactFlow,
  type Edge,
  type Node,
  type NodeMouseHandler,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";

import type { IntegrationFlowPipelineDefinition } from "@/features/smartconnect/api/types";

export type PipelineColumn = "filter" | "transform" | "outbound";

interface PipelineGraphProps {
  pipeline: IntegrationFlowPipelineDefinition;
  /** Currently-selected node id (e.g. `filter-0`). When set, the node gets a brighter border. */
  selectedNodeId?: string | null;
  /** Called when the operator clicks a real node (not the `+` placeholders). `null` clears. */
  onSelectNode?: (id: string | null) => void;
  /** Called when the operator clicks a column's `+` placeholder. Activates the placeholders. */
  onAddNode?: (column: PipelineColumn) => void;
}

const COLUMN_X = {
  source: 40,
  filter: 260,
  transform: 480,
  outbound: 700,
} as const;
const ROW_HEIGHT = 90;
const NODE_BASE_WIDTH = 180;

const nodeStyle = (tint: string, selected: boolean): Node["style"] => ({
  background: tint,
  border: selected ? "2px solid #38bdf8" : "1px solid #475569",
  color: "#f1f5f9",
  borderRadius: 4,
  padding: 8,
  fontSize: 11,
  width: NODE_BASE_WIDTH,
  boxShadow: selected ? "0 0 0 2px rgba(56,189,248,0.25)" : undefined,
});

const placeholderStyle: Node["style"] = {
  background: "#0b1220",
  border: "1px dashed #64748b",
  color: "#94a3b8",
  borderRadius: 4,
  padding: 8,
  fontSize: 11,
  width: NODE_BASE_WIDTH,
  textAlign: "center",
};

export function PipelineGraph({
  pipeline,
  selectedNodeId = null,
  onSelectNode,
  onAddNode,
}: PipelineGraphProps): JSX.Element {
  const { nodes, edges } = useMemo(
    () => buildGraph(pipeline, selectedNodeId, Boolean(onAddNode)),
    [pipeline, selectedNodeId, onAddNode],
  );

  const handleNodeClick = useCallback<NodeMouseHandler>(
    (_event, node) => {
      if (node.id.startsWith("add-")) {
        if (!onAddNode) return;
        if (node.id === "add-filter") onAddNode("filter");
        else if (node.id === "add-transform") onAddNode("transform");
        else if (node.id === "add-outbound") onAddNode("outbound");
        return;
      }
      onSelectNode?.(node.id);
    },
    [onAddNode, onSelectNode],
  );

  const handlePaneClick = useCallback(() => {
    onSelectNode?.(null);
  }, [onSelectNode]);

  return (
    <div className="h-[480px] w-full rounded border border-slate-700 bg-slate-950">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        fitView
        nodesDraggable={false}
        nodesConnectable={false}
        elementsSelectable
        onNodeClick={onSelectNode || onAddNode ? handleNodeClick : undefined}
        onPaneClick={onSelectNode ? handlePaneClick : undefined}
        proOptions={{ hideAttribution: true }}
      >
        <Background gap={16} color="#1e293b" />
        <Controls showInteractive={false} />
      </ReactFlow>
    </div>
  );
}

function buildGraph(
  pipeline: IntegrationFlowPipelineDefinition,
  selectedNodeId: string | null,
  showPlaceholders: boolean,
): {
  nodes: Node[];
  edges: Edge[];
} {
  const nodes: Node[] = [];
  const edges: Edge[] = [];

  const sourceId = "source";
  nodes.push({
    id: sourceId,
    position: { x: COLUMN_X.source, y: 0 },
    data: { label: <strong>Source</strong> },
    style: nodeStyle("#0f172a", false),
  });

  const filterIds = pipeline.routeFilters.map((_, idx) => `filter-${idx}`);
  pipeline.routeFilters.forEach((slot, idx) => {
    nodes.push({
      id: filterIds[idx]!,
      position: { x: COLUMN_X.filter, y: idx * ROW_HEIGHT },
      data: { label: <NodeBody kind={slot.kind} stageRole="Route filter" /> },
      style: nodeStyle("#1e293b", selectedNodeId === filterIds[idx]),
    });
  });

  const transformIds = pipeline.sourceTransformStages.map((_, idx) => `transform-${idx}`);
  pipeline.sourceTransformStages.forEach((slot, idx) => {
    nodes.push({
      id: transformIds[idx]!,
      position: { x: COLUMN_X.transform, y: idx * ROW_HEIGHT },
      data: { label: <NodeBody kind={slot.kind} stageRole="Transform" /> },
      style: nodeStyle("#1e293b", selectedNodeId === transformIds[idx]),
    });
  });

  const outboundIds = pipeline.outboundRoutes.map((_, idx) => `outbound-${idx}`);
  pipeline.outboundRoutes.forEach((slot, idx) => {
    nodes.push({
      id: outboundIds[idx]!,
      position: { x: COLUMN_X.outbound, y: idx * ROW_HEIGHT },
      data: { label: <NodeBody kind={slot.kind} stageRole="Outbound route" /> },
      style: nodeStyle("#172033", selectedNodeId === outboundIds[idx]),
    });
  });

  if (showPlaceholders) {
    nodes.push({
      id: "add-filter",
      position: { x: COLUMN_X.filter, y: pipeline.routeFilters.length * ROW_HEIGHT },
      data: { label: <span>+ Add filter</span> },
      style: placeholderStyle,
    });
    nodes.push({
      id: "add-transform",
      position: { x: COLUMN_X.transform, y: pipeline.sourceTransformStages.length * ROW_HEIGHT },
      data: { label: <span>+ Add transform</span> },
      style: placeholderStyle,
    });
    nodes.push({
      id: "add-outbound",
      position: { x: COLUMN_X.outbound, y: pipeline.outboundRoutes.length * ROW_HEIGHT },
      data: { label: <span>+ Add route</span> },
      style: placeholderStyle,
    });
  }

  // Wire edges: source → first filter → … → first transform → … → each outbound.
  let lastUpstream = sourceId;
  for (const id of filterIds) {
    edges.push(makeEdge(`${lastUpstream}-${id}`, lastUpstream, id));
    lastUpstream = id;
  }
  for (const id of transformIds) {
    edges.push(makeEdge(`${lastUpstream}-${id}`, lastUpstream, id));
    lastUpstream = id;
  }
  if (pipeline.outboundRoutesSequential) {
    for (const id of outboundIds) {
      edges.push(makeEdge(`${lastUpstream}-${id}`, lastUpstream, id));
      lastUpstream = id;
    }
  } else {
    for (const id of outboundIds) {
      edges.push(makeEdge(`${lastUpstream}-${id}`, lastUpstream, id));
    }
  }

  return { nodes, edges };
}

function makeEdge(id: string, source: string, target: string): Edge {
  return {
    id,
    source,
    target,
    type: "smoothstep",
    markerEnd: { type: MarkerType.ArrowClosed, color: "#94a3b8" },
    style: { stroke: "#94a3b8" },
  };
}

// `stageRole`, not `role`: a `role` prop on a JSX element is reserved for ARIA roles
// (jsx-a11y/aria-role), and these are pipeline-stage captions, not ARIA roles.
function NodeBody({ kind, stageRole }: { kind: string; stageRole: string }): JSX.Element {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-[10px] uppercase tracking-wider text-slate-400">{stageRole}</span>
      <strong className="text-xs text-slate-100">{kind}</strong>
    </div>
  );
}
