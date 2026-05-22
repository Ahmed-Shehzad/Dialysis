// Slice G2 of the SmartConnect ↔ Mirth alignment plan: read-only React Flow rendering of
// an IntegrationFlowPipelineDefinition. Operators see the source → filters → transforms →
// routes shape at a glance and click an outbound node to follow the connector-property
// schema (slice B2) into a per-route form. Edits still round-trip through the JSON view
// that ships in slice G's scaffold; G2 is purely a visualisation pass.

import { useMemo } from "react";
import { Background, Controls, MarkerType, ReactFlow, type Edge, type Node } from "@xyflow/react";
import "@xyflow/react/dist/style.css";

import type { IntegrationFlowPipelineDefinition } from "@/features/smartconnect/api/types";

interface PipelineGraphProps {
  pipeline: IntegrationFlowPipelineDefinition;
}

const COLUMN_X = {
  source: 40,
  filter: 260,
  transform: 480,
  outbound: 700,
} as const;
const ROW_HEIGHT = 90;
const NODE_BASE_WIDTH = 180;

const nodeStyle = (tint: string): Node["style"] => ({
  background: tint,
  border: "1px solid #475569",
  color: "#f1f5f9",
  borderRadius: 4,
  padding: 8,
  fontSize: 11,
  width: NODE_BASE_WIDTH,
});

export function PipelineGraph({ pipeline }: PipelineGraphProps): JSX.Element {
  const { nodes, edges } = useMemo(() => buildGraph(pipeline), [pipeline]);

  return (
    <div className="h-[480px] w-full rounded border border-slate-700 bg-slate-950">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        fitView
        nodesDraggable={false}
        nodesConnectable={false}
        elementsSelectable
        proOptions={{ hideAttribution: true }}
      >
        <Background gap={16} color="#1e293b" />
        <Controls showInteractive={false} />
      </ReactFlow>
    </div>
  );
}

function buildGraph(pipeline: IntegrationFlowPipelineDefinition): {
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
    style: nodeStyle("#0f172a"),
  });

  // Filters column — each route filter slot is one node, stacked vertically.
  const filterIds = pipeline.routeFilters.map((slot, idx) => `filter-${idx}`);
  pipeline.routeFilters.forEach((slot, idx) => {
    nodes.push({
      id: filterIds[idx]!,
      position: { x: COLUMN_X.filter, y: idx * ROW_HEIGHT },
      data: { label: <NodeBody kind={slot.kind} role="Route filter" /> },
      style: nodeStyle("#1e293b"),
    });
  });

  // Source-side transform stages column.
  const transformIds = pipeline.sourceTransformStages.map((_, idx) => `transform-${idx}`);
  pipeline.sourceTransformStages.forEach((slot, idx) => {
    nodes.push({
      id: transformIds[idx]!,
      position: { x: COLUMN_X.transform, y: idx * ROW_HEIGHT },
      data: { label: <NodeBody kind={slot.kind} role="Transform" /> },
      style: nodeStyle("#1e293b"),
    });
  });

  // Outbound routes column — one node per route. Sequential vs. parallel is encoded by
  // the edges (a sequential pipeline chains routes 0 → 1 → 2; parallel fans out from
  // the last upstream node into each route).
  const outboundIds = pipeline.outboundRoutes.map((_, idx) => `outbound-${idx}`);
  pipeline.outboundRoutes.forEach((slot, idx) => {
    nodes.push({
      id: outboundIds[idx]!,
      position: { x: COLUMN_X.outbound, y: idx * ROW_HEIGHT },
      data: { label: <NodeBody kind={slot.outboundAdapterKind} role="Outbound route" /> },
      style: nodeStyle("#172033"),
    });
  });

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
    // Parallel — every outbound branches from the last upstream node.
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

function NodeBody({ kind, role }: { kind: string; role: string }): JSX.Element {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-[10px] uppercase tracking-wider text-slate-400">{role}</span>
      <strong className="text-xs text-slate-100">{kind}</strong>
    </div>
  );
}
