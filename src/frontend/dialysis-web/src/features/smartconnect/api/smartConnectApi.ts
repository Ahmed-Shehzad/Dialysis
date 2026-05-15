import { apiClient } from "@/lib/api/apiClient";

export type FlowSummary = {
  id: string;
  name: string;
  state: string;
  enabled?: boolean;
  description?: string | null;
};

export type MessageRow = {
  id: string;
  flowId: string;
  status: string;
  receivedAtUtc: string;
  correlationId?: string | null;
};

// SmartConnect serializes the enums as integers (System.Text.Json default) and uses different
// field names from what the SPA renders — runtimeState/createdAtUtc/numeric status. Mapping
// happens here so the page stays a dumb projection and the wire shape never leaks upward.

type FlowRuntimeStateWire = 0 | 1 | 2;
const FLOW_STATE: Record<FlowRuntimeStateWire, string> = {
  0: "Stopped",
  1: "Started",
  2: "Paused",
};

type MessageStatusWire = 0 | 1 | 2 | 3 | 4;
const MESSAGE_STATUS: Record<MessageStatusWire, string> = {
  0: "Received",
  1: "RouteFilterDropped",
  2: "OutboundSent",
  3: "OutboundFailed",
  4: "Completed",
};

type FlowWire = {
  id: string;
  name: string;
  runtimeState?: number;
  state?: string;
  enabled?: boolean;
  description?: string | null;
};

type MessageWire = {
  id: string;
  flowId: string;
  status?: number | string;
  receivedAtUtc?: string;
  createdAtUtc?: string;
  correlationId?: string | null;
};

const resolveFlowState = (f: FlowWire): string => {
  if (typeof f.state === "string") return f.state;
  if (f.runtimeState !== undefined && f.runtimeState in FLOW_STATE) {
    return FLOW_STATE[f.runtimeState as FlowRuntimeStateWire];
  }
  return "Unknown";
};

const mapFlow = (f: FlowWire): FlowSummary => ({
  id: f.id,
  name: f.name,
  state: resolveFlowState(f),
  enabled: f.enabled,
  description: f.description,
});

const mapMessage = (m: MessageWire): MessageRow => ({
  id: m.id,
  flowId: m.flowId,
  status:
    typeof m.status === "number" && m.status in MESSAGE_STATUS
      ? MESSAGE_STATUS[m.status as MessageStatusWire]
      : (m.status as string | undefined) ?? "Unknown",
  receivedAtUtc: m.receivedAtUtc ?? m.createdAtUtc ?? new Date(0).toISOString(),
  correlationId: m.correlationId,
});

export const fetchFlows = async (): Promise<FlowSummary[]> => {
  const response = await apiClient.get<FlowWire[]>(
    "/api/smartconnect/smartconnect/v1/admin/flows",
  );
  return (response.data ?? []).map(mapFlow);
};

export const fetchRecentMessages = async (take = 25): Promise<MessageRow[]> => {
  const response = await apiClient.get<MessageWire[] | { items?: MessageWire[] }>(
    "/api/smartconnect/smartconnect/v1/admin/messages",
    { params: { take } },
  );
  const body = response.data;
  const items = Array.isArray(body) ? body : body?.items ?? [];
  return items.map(mapMessage);
};

export const startFlow = (flowId: string) =>
  apiClient.post(`/api/smartconnect/smartconnect/v1/admin/flows/${flowId}/start`);

export const stopFlow = (flowId: string) =>
  apiClient.post(`/api/smartconnect/smartconnect/v1/admin/flows/${flowId}/stop`);

export const pauseFlow = (flowId: string) =>
  apiClient.post(`/api/smartconnect/smartconnect/v1/admin/flows/${flowId}/pause`);

export const reprocessMessage = (messageId: string) =>
  apiClient.post(`/api/smartconnect/smartconnect/v1/admin/messages/${messageId}/reprocess`);
