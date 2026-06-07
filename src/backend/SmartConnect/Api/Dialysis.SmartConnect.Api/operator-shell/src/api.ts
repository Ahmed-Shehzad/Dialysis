import { getToken } from "./auth";

export interface ApiError extends Error {
  status: number;
}

export function apiUrl(path: string): string {
  return path.startsWith("/") ? path : `/${path}`;
}

async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const headers = new Headers(init?.headers ?? {});
  const token = getToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const res = await fetch(apiUrl(path), { ...init, headers });
  if (!res.ok) {
    const err = new Error(`${res.status} ${res.statusText} for ${path}`) as ApiError;
    err.status = res.status;
    throw err;
  }
  return res;
}

async function apiJson<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await apiFetch(path, init);
  if (res.status === 204) return undefined as unknown as T;
  return res.json() as Promise<T>;
}

// ---- Flows ----
export interface FlowSummary { id: string; name?: string; runtimeState?: string }
export function listFlows(): Promise<FlowSummary[]> {
  return apiJson<FlowSummary[]>("/api/v1/admin/flows");
}

// ---- Messages ----
export interface MessageRow {
  id: string;
  createdAtUtc?: string;
  status?: number | string;
  correlationId?: string;
  detail?: string;
}
export interface MessageListResponse { items: MessageRow[] }

export function listMessages(opts: { flowId?: string; status?: string; take?: number }): Promise<MessageListResponse> {
  const params = new URLSearchParams();
  params.set("take", String(opts.take ?? 30));
  if (opts.flowId) params.set("flowId", opts.flowId);
  if (opts.status) params.set("status", opts.status);
  return apiJson<MessageListResponse>(`/api/v1/admin/messages?${params}`);
}

export async function reprocessMessage(id: string): Promise<void> {
  await apiFetch(`/api/v1/admin/messages/${encodeURIComponent(id)}/reprocess`, { method: "POST" });
}

// ---- Attachments ----
export interface AttachmentMetadata {
  id: string;
  messageId?: string;
  flowId?: string;
  mimeType?: string;
  sizeBytes?: number;
  createdUtc?: string;
}
export function listAttachmentsForMessage(messageId: string): Promise<AttachmentMetadata[]> {
  return apiJson<AttachmentMetadata[]>(`/api/v1/admin/messages/${encodeURIComponent(messageId)}/attachments`);
}
export function downloadAttachmentUrl(id: string): string {
  return `/api/v1/admin/attachments/${encodeURIComponent(id)}`;
}
/** Slice I: fetches the attachment payload bytes for an inline preview. */
export async function fetchAttachmentBytes(id: string): Promise<Uint8Array> {
  const res = await apiFetch(`/api/v1/admin/attachments/${encodeURIComponent(id)}`);
  const buf = await res.arrayBuffer();
  return new Uint8Array(buf);
}
export async function deleteAttachment(id: string): Promise<void> {
  await apiFetch(`/api/v1/admin/attachments/${encodeURIComponent(id)}`, { method: "DELETE" });
}

// ---- Alerts ----
export interface AlertRuleSummary {
  id: string;
  name?: string;
  enabled?: boolean;
  description?: string;
}
export interface AlertActionOutcome {
  kind?: string;
  succeeded?: boolean;
  errorDetail?: string;
  responseSummary?: string;
  attemptedAtUtc?: string;
}
export interface AlertEventRow {
  id: string;
  ruleId?: string;
  flowId?: string;
  errorType?: string | number;
  errorDetail?: string;
  occurredAtUtc?: string;
  actionOutcomes?: AlertActionOutcome[];
}

export function listAlertRules(): Promise<AlertRuleSummary[]> {
  return apiJson<AlertRuleSummary[]>("/api/v1/admin/alert-rules");
}
export function getAlertRule(id: string): Promise<AlertRuleSummary & Record<string, unknown>> {
  return apiJson(`/api/v1/admin/alert-rules/${encodeURIComponent(id)}`);
}
export function listAlertEvents(opts: { ruleId?: string; take?: number } = {}): Promise<AlertEventRow[]> {
  const params = new URLSearchParams();
  params.set("take", String(opts.take ?? 30));
  if (opts.ruleId) params.set("ruleId", opts.ruleId);
  return apiJson<AlertEventRow[]>(`/api/v1/admin/alert-events?${params}`);
}
export function testAlertRule(id: string): Promise<AlertActionOutcome[]> {
  return apiJson<AlertActionOutcome[]>(`/api/v1/admin/alert-rules/${encodeURIComponent(id)}/test`, { method: "POST" });
}

// ---- Code template libraries ----
export interface CodeTemplate {
  id?: string;
  name?: string;
  body?: string;
  contextString?: string;
}
export interface CodeTemplateLibrary {
  id: string;
  name?: string;
  description?: string;
  templates?: CodeTemplate[];
}
export function listCodeTemplateLibraries(): Promise<CodeTemplateLibrary[]> {
  return apiJson<CodeTemplateLibrary[]>("/api/v1/admin/code-template-libraries");
}
export function getCodeTemplateLibrary(id: string): Promise<CodeTemplateLibrary> {
  return apiJson<CodeTemplateLibrary>(`/api/v1/admin/code-template-libraries/${encodeURIComponent(id)}`);
}

// ---- Variable maps ----
export type VariableMapScope = "Global" | "GlobalChannel" | "Configuration" | "Channel" | "Source" | "Connector" | "Response";

export function getConfigMap(scope: VariableMapScope, flowId?: string): Promise<Record<string, string>> {
  const path = `/api/v1/admin/config-map/${encodeURIComponent(scope)}`;
  const url = flowId ? `${path}?flowId=${encodeURIComponent(flowId)}` : path;
  return apiJson<Record<string, string>>(url);
}
export async function setConfigMapValue(scope: VariableMapScope, key: string, value: string, flowId?: string): Promise<void> {
  const path = `/api/v1/admin/config-map/${encodeURIComponent(scope)}/${encodeURIComponent(key)}`;
  const url = flowId ? `${path}?flowId=${encodeURIComponent(flowId)}` : path;
  await apiFetch(url, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ value }),
  });
}
export async function deleteConfigMapValue(scope: VariableMapScope, key: string, flowId?: string): Promise<void> {
  const path = `/api/v1/admin/config-map/${encodeURIComponent(scope)}/${encodeURIComponent(key)}`;
  const url = flowId ? `${path}?flowId=${encodeURIComponent(flowId)}` : path;
  await apiFetch(url, { method: "DELETE" });
}

// ---- Pruner ----
export function getPrunerOptions(): Promise<unknown> {
  return apiJson<unknown>("/api/v1/admin/pruner/options");
}

// ---- Health ----
export function checkHealth(): Promise<Response> {
  return apiFetch("/health");
}
