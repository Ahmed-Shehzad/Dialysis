import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchFlows } from "../api/flows";
import { FlowRuntimeState, type FlowRuntimeStateValue, type IntegrationFlow } from "../api/types";

// Pure SVG dependency-graph renderer. Lays out flows in columns by depth (root deps on the left,
// dependents on the right), then draws arrows along the dependency edges. Deliberately no
// Mermaid / Cytoscape / dagre — keeps the bundle lean and avoids a layout engine for what is at
// most a few dozen channels per environment.

type GraphNode = {
  flow: IntegrationFlow;
  depth: number;
  x: number;
  y: number;
};

const NODE_WIDTH = 220;
const NODE_HEIGHT = 64;
const COL_GAP = 80;
const ROW_GAP = 24;
const MARGIN = 32;

const stateColor = (state: FlowRuntimeStateValue): string => {
  switch (state) {
    case FlowRuntimeState.Started:
      return "#34d399"; // emerald-400
    case FlowRuntimeState.Paused:
      return "#fbbf24"; // amber-400
    case FlowRuntimeState.Stopped:
    default:
      return "#94a3b8"; // slate-400
  }
};

const computeLayout = (flows: IntegrationFlow[]): GraphNode[] => {
  const byId = new Map(flows.map((f) => [f.id, f] as const));
  const depth = new Map<string, number>();
  const visiting = new Set<string>();

  const resolveDepth = (id: string): number => {
    if (depth.has(id)) return depth.get(id)!;
    if (visiting.has(id)) return 0; // cycle guard — treat as root for layout purposes
    visiting.add(id);
    const flow = byId.get(id);
    const deps = flow?.dependencies ?? [];
    const d = deps.length === 0 ? 0 : 1 + Math.max(...deps.map(resolveDepth));
    visiting.delete(id);
    depth.set(id, d);
    return d;
  };

  flows.forEach((f) => resolveDepth(f.id));

  // Group by column.
  const columns = new Map<number, IntegrationFlow[]>();
  flows.forEach((f) => {
    const d = depth.get(f.id) ?? 0;
    if (!columns.has(d)) columns.set(d, []);
    columns.get(d)!.push(f);
  });

  const nodes: GraphNode[] = [];
  Array.from(columns.entries())
    .sort(([a], [b]) => a - b)
    .forEach(([col, items]) => {
      items
        .sort((a, b) => a.name.localeCompare(b.name))
        .forEach((flow, row) => {
          nodes.push({
            flow,
            depth: col,
            x: MARGIN + col * (NODE_WIDTH + COL_GAP),
            y: MARGIN + row * (NODE_HEIGHT + ROW_GAP),
          });
        });
    });

  return nodes;
};

export const DependencyGraphTab = () => {
  const {
    data: flows = [],
    isLoading,
    error,
  } = useQuery({
    queryKey: ["smartconnect", "flows"],
    queryFn: fetchFlows,
    refetchInterval: 30_000,
  });

  const layout = useMemo(() => computeLayout(flows), [flows]);
  const nodeById = useMemo(() => new Map(layout.map((n) => [n.flow.id, n] as const)), [layout]);

  if (isLoading) {
    return <p className="text-sm text-slate-400">Loading channels…</p>;
  }
  if (error) {
    return (
      <p className="text-sm text-rose-300">Failed to load channels: {(error as Error).message}</p>
    );
  }
  if (flows.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        No channels yet — create one from the Flows tab to start building the graph.
      </p>
    );
  }

  const width = Math.max(...layout.map((n) => n.x + NODE_WIDTH), MARGIN + NODE_WIDTH) + MARGIN;
  const height = Math.max(...layout.map((n) => n.y + NODE_HEIGHT), MARGIN + NODE_HEIGHT) + MARGIN;

  return (
    <div className="space-y-3">
      <p className="text-sm text-slate-400">
        Arrows point from a channel to the channels it depends on. Stopped dependencies block the
        dependent's Start unless <code className="rounded bg-slate-800 px-1">?force=true</code> or
        <code className="ml-1 rounded bg-slate-800 px-1">?cascade=true</code> is used.
      </p>
      <div className="overflow-auto rounded border border-slate-800 bg-slate-900/40 p-2">
        <svg width={width} height={height} className="block">
          <defs>
            <marker
              id="dep-arrow"
              viewBox="0 0 10 10"
              refX="9"
              refY="5"
              markerWidth="6"
              markerHeight="6"
              orient="auto-start-reverse"
            >
              <path d="M 0 0 L 10 5 L 0 10 z" fill="#64748b" />
            </marker>
          </defs>

          {/* Edges first so nodes draw on top. */}
          {layout.flatMap((n) =>
            n.flow.dependencies.map((depId) => {
              const target = nodeById.get(depId);
              if (!target) return null;
              const x1 = n.x;
              const y1 = n.y + NODE_HEIGHT / 2;
              const x2 = target.x + NODE_WIDTH;
              const y2 = target.y + NODE_HEIGHT / 2;
              const cx = (x1 + x2) / 2;
              return (
                <path
                  key={`${n.flow.id}->${depId}`}
                  d={`M ${x1} ${y1} C ${cx} ${y1}, ${cx} ${y2}, ${x2} ${y2}`}
                  stroke="#64748b"
                  strokeWidth={1.5}
                  fill="none"
                  markerEnd="url(#dep-arrow)"
                />
              );
            }),
          )}

          {layout.map((n) => (
            <g key={n.flow.id} transform={`translate(${n.x}, ${n.y})`}>
              <rect
                width={NODE_WIDTH}
                height={NODE_HEIGHT}
                rx={6}
                fill="#0f172a"
                stroke={stateColor(n.flow.runtimeState)}
                strokeWidth={1.5}
              />
              <text
                x={12}
                y={22}
                fill="#e2e8f0"
                fontSize={13}
                fontFamily="ui-sans-serif, system-ui, sans-serif"
              >
                {n.flow.name.length > 26 ? `${n.flow.name.slice(0, 25)}…` : n.flow.name}
              </text>
              <text x={12} y={42} fill="#94a3b8" fontSize={11}>
                {FlowRuntimeStateName(n.flow.runtimeState)} · {n.flow.dataTypes?.join(", ") || "—"}
              </text>
              <text x={12} y={58} fill="#64748b" fontSize={10}>
                {n.flow.dependencies.length === 0
                  ? "no deps"
                  : `${n.flow.dependencies.length} dep${n.flow.dependencies.length === 1 ? "" : "s"}`}
              </text>
            </g>
          ))}
        </svg>
      </div>
    </div>
  );
};

const FlowRuntimeStateName = (state: FlowRuntimeStateValue): string => {
  switch (state) {
    case FlowRuntimeState.Started:
      return "Started";
    case FlowRuntimeState.Paused:
      return "Paused";
    case FlowRuntimeState.Stopped:
    default:
      return "Stopped";
  }
};
