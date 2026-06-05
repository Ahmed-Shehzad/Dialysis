// Side drawer for the visual pipeline editor (slice G2 interactive). Shows the kind selector,
// the structured AdapterParametersForm for the chosen kind, reorder chevrons, and a Remove
// button. Mutations bubble back to ChannelEditorPage via the supplied callbacks; this component
// holds no local pipeline state.

import { AdapterParametersForm } from "./AdapterParametersForm";
import { getRouteFilterSchema, getSchema, getTransformStageSchema } from "../api/adapterSchemas";
import type {
  IntegrationFlowPipelineDefinition,
  OutboundRouteSlot,
  RouteFilterSlot,
  TransformStageSlot,
} from "../api/types";
import type { PipelineColumn } from "./PipelineGraph";

export type SelectedNode = {
  column: PipelineColumn;
  index: number;
};

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

const OUTBOUND_KINDS = [
  "transponder-bus",
  "http",
  "tcp",
  "file",
  "smtp",
  "database",
  "channel-writer",
  "pass-through",
] as const;

type PipelineNodeDrawerProps = {
  pipeline: IntegrationFlowPipelineDefinition;
  selection: SelectedNode;
  onChangePipeline: (next: IntegrationFlowPipelineDefinition) => void;
  onClose: () => void;
};

export function PipelineNodeDrawer({
  pipeline,
  selection,
  onChangePipeline,
  onClose,
}: PipelineNodeDrawerProps): JSX.Element {
  const slot = readSlot(pipeline, selection);
  if (!slot) {
    return (
      <aside className="border-l border-slate-800 bg-slate-900 p-4 text-xs text-slate-400">
        Selection is stale. Click a node to edit.
      </aside>
    );
  }

  const updateKind = (kind: string) => {
    onChangePipeline(writeSlot(pipeline, selection, { ...slot, kind, propertiesJson: null }));
  };

  const updateProperties = (propertiesJson: string | null) => {
    onChangePipeline(writeSlot(pipeline, selection, { ...slot, propertiesJson }));
  };

  const remove = () => {
    onChangePipeline(deleteSlot(pipeline, selection));
    onClose();
  };

  const move = (delta: -1 | 1) => {
    const next = reorderSlot(pipeline, selection, delta);
    if (next) onChangePipeline(next);
  };

  const { kinds, schemaResolver, roleLabel, columnSize } = columnConfig(pipeline, selection.column);

  return (
    <aside className="flex w-80 flex-col gap-3 border-l border-slate-800 bg-slate-900 p-4 text-xs text-slate-200">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-clinic-100">
          {roleLabel} #{selection.index}
        </h3>
        <button
          type="button"
          onClick={onClose}
          className="rounded border border-slate-700 px-2 py-0.5 text-[11px] text-slate-300 hover:bg-slate-800"
          aria-label="Close drawer"
        >
          Close
        </button>
      </header>

      <label className="block">
        <span className="text-[11px] text-slate-400">Kind</span>
        <select
          value={slot.kind}
          onChange={(e) => updateKind(e.target.value)}
          className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-2 py-1 text-xs text-slate-100"
        >
          {kinds.map((k) => (
            <option key={k} value={k}>
              {k}
            </option>
          ))}
          {!kinds.includes(slot.kind as never) && (
            <option value={slot.kind}>{slot.kind} (unregistered)</option>
          )}
        </select>
      </label>

      <AdapterParametersForm
        kind={slot.kind}
        propertiesJson={slot.propertiesJson ?? null}
        onChange={updateProperties}
        schemaResolver={schemaResolver}
      />

      <div className="flex items-center gap-2 border-t border-slate-800 pt-3">
        <button
          type="button"
          disabled={selection.index === 0}
          onClick={() => move(-1)}
          className="rounded border border-slate-700 px-2 py-0.5 text-[11px] text-slate-300 hover:bg-slate-800 disabled:opacity-40"
          aria-label="Move up"
        >
          ↑ Up
        </button>
        <button
          type="button"
          disabled={selection.index >= columnSize - 1}
          onClick={() => move(1)}
          className="rounded border border-slate-700 px-2 py-0.5 text-[11px] text-slate-300 hover:bg-slate-800 disabled:opacity-40"
          aria-label="Move down"
        >
          ↓ Down
        </button>
        <span className="ml-auto" />
        <button
          type="button"
          onClick={remove}
          className="rounded border border-rose-700 px-2 py-0.5 text-[11px] text-rose-300 hover:bg-rose-900/40"
        >
          Remove
        </button>
      </div>
    </aside>
  );
}

function readSlot(
  pipeline: IntegrationFlowPipelineDefinition,
  selection: SelectedNode,
): RouteFilterSlot | TransformStageSlot | OutboundRouteSlot | null {
  switch (selection.column) {
    case "filter":
      return pipeline.routeFilters[selection.index] ?? null;
    case "transform":
      return pipeline.sourceTransformStages[selection.index] ?? null;
    case "outbound":
      return pipeline.outboundRoutes[selection.index] ?? null;
  }
}

function writeSlot(
  pipeline: IntegrationFlowPipelineDefinition,
  selection: SelectedNode,
  next: RouteFilterSlot | TransformStageSlot | OutboundRouteSlot,
): IntegrationFlowPipelineDefinition {
  switch (selection.column) {
    case "filter":
      return {
        ...pipeline,
        routeFilters: pipeline.routeFilters.map((s, i) =>
          i === selection.index ? (next as RouteFilterSlot) : s,
        ),
      };
    case "transform":
      return {
        ...pipeline,
        sourceTransformStages: pipeline.sourceTransformStages.map((s, i) =>
          i === selection.index ? (next as TransformStageSlot) : s,
        ),
      };
    case "outbound":
      return {
        ...pipeline,
        outboundRoutes: pipeline.outboundRoutes.map((s, i) =>
          i === selection.index ? { ...(next as OutboundRouteSlot), ordinal: i } : s,
        ),
      };
  }
}

function deleteSlot(
  pipeline: IntegrationFlowPipelineDefinition,
  selection: SelectedNode,
): IntegrationFlowPipelineDefinition {
  switch (selection.column) {
    case "filter":
      return {
        ...pipeline,
        routeFilters: pipeline.routeFilters.filter((_, i) => i !== selection.index),
      };
    case "transform":
      return {
        ...pipeline,
        sourceTransformStages: pipeline.sourceTransformStages.filter(
          (_, i) => i !== selection.index,
        ),
      };
    case "outbound":
      return {
        ...pipeline,
        outboundRoutes: pipeline.outboundRoutes
          .filter((_, i) => i !== selection.index)
          .map((r, i) => ({ ...r, ordinal: i })),
      };
  }
}

function reorderSlot(
  pipeline: IntegrationFlowPipelineDefinition,
  selection: SelectedNode,
  delta: -1 | 1,
): IntegrationFlowPipelineDefinition | null {
  const target = selection.index + delta;
  switch (selection.column) {
    case "filter": {
      const arr = pipeline.routeFilters.slice();
      if (target < 0 || target >= arr.length) return null;
      [arr[selection.index], arr[target]] = [arr[target]!, arr[selection.index]!];
      return { ...pipeline, routeFilters: arr };
    }
    case "transform": {
      const arr = pipeline.sourceTransformStages.slice();
      if (target < 0 || target >= arr.length) return null;
      [arr[selection.index], arr[target]] = [arr[target]!, arr[selection.index]!];
      return { ...pipeline, sourceTransformStages: arr };
    }
    case "outbound": {
      const arr = pipeline.outboundRoutes.slice();
      if (target < 0 || target >= arr.length) return null;
      [arr[selection.index], arr[target]] = [arr[target]!, arr[selection.index]!];
      return {
        ...pipeline,
        outboundRoutes: arr.map((r, i) => ({ ...r, ordinal: i })),
      };
    }
  }
}

function columnConfig(
  pipeline: IntegrationFlowPipelineDefinition,
  column: PipelineColumn,
): {
  kinds: readonly string[];
  schemaResolver: typeof getSchema;
  roleLabel: string;
  columnSize: number;
} {
  switch (column) {
    case "filter":
      return {
        kinds: ROUTE_FILTER_KINDS,
        schemaResolver: getRouteFilterSchema,
        roleLabel: "Route filter",
        columnSize: pipeline.routeFilters.length,
      };
    case "transform":
      return {
        kinds: TRANSFORM_STAGE_KINDS,
        schemaResolver: getTransformStageSchema,
        roleLabel: "Source transform",
        columnSize: pipeline.sourceTransformStages.length,
      };
    case "outbound":
      return {
        kinds: OUTBOUND_KINDS,
        schemaResolver: getSchema,
        roleLabel: "Outbound route",
        columnSize: pipeline.outboundRoutes.length,
      };
  }
}
