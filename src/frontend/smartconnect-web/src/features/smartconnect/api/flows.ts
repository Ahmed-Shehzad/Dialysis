import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type FlowStatusCount, type IntegrationFlow } from "./types";

export const fetchFlows = async (): Promise<IntegrationFlow[]> => {
  const res = await apiClient.get<IntegrationFlow[]>(`${ADMIN_PREFIX}/flows`);
  return res.data ?? [];
};

export const fetchFlow = async (flowId: string): Promise<IntegrationFlow> => {
  const res = await apiClient.get<IntegrationFlow>(`${ADMIN_PREFIX}/flows/${flowId}`);
  return res.data;
};

export const createFlow = async (flow: IntegrationFlow): Promise<IntegrationFlow> => {
  const res = await apiClient.post<IntegrationFlow>(`${ADMIN_PREFIX}/flows`, flow);
  return res.data;
};

export const updateFlow = async (flow: IntegrationFlow): Promise<void> => {
  await apiClient.put(`${ADMIN_PREFIX}/flows/${flow.id}`, flow);
};

export const deleteFlow = async (flowId: string): Promise<void> => {
  await apiClient.delete(`${ADMIN_PREFIX}/flows/${flowId}`);
};

export const startFlow = (flowId: string) =>
  apiClient.post(`${ADMIN_PREFIX}/flows/${flowId}/start`);
export const stopFlow = (flowId: string) => apiClient.post(`${ADMIN_PREFIX}/flows/${flowId}/stop`);
export const pauseFlow = (flowId: string) =>
  apiClient.post(`${ADMIN_PREFIX}/flows/${flowId}/pause`);

export const importFlow = async (flow: IntegrationFlow): Promise<IntegrationFlow> => {
  const res = await apiClient.post<IntegrationFlow>(`${ADMIN_PREFIX}/flows/import`, flow);
  return res.data;
};

export const exportFlow = async (flowId: string): Promise<IntegrationFlow> => {
  const res = await apiClient.get<IntegrationFlow>(`${ADMIN_PREFIX}/flows/${flowId}/export`);
  return res.data;
};

export const fetchFlowStatistics = async (flowId: string): Promise<FlowStatusCount[]> => {
  const res = await apiClient.get<FlowStatusCount[]>(`${ADMIN_PREFIX}/flows/${flowId}/statistics`);
  return res.data ?? [];
};
