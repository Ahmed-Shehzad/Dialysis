import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type AlertEvent, type AlertRule, type TestAlertRequest } from "./types";

const RULES = `${ADMIN_PREFIX}/alert-rules`;
const EVENTS = `${ADMIN_PREFIX}/alert-events`;

export const fetchAlertRules = async (enabledOnly = false): Promise<AlertRule[]> => {
  const res = await apiClient.get<AlertRule[]>(RULES, { params: { enabledOnly } });
  return res.data ?? [];
};

export const fetchAlertRule = async (id: string): Promise<AlertRule> => {
  const res = await apiClient.get<AlertRule>(`${RULES}/${id}`);
  return res.data;
};

export const createAlertRule = async (rule: AlertRule): Promise<AlertRule> => {
  const res = await apiClient.post<AlertRule>(RULES, rule);
  return res.data;
};

export const updateAlertRule = async (rule: AlertRule): Promise<AlertRule> => {
  const res = await apiClient.put<AlertRule>(`${RULES}/${rule.id}`, rule);
  return res.data;
};

export const deleteAlertRule = async (id: string): Promise<void> => {
  await apiClient.delete(`${RULES}/${id}`);
};

export const testAlertRule = async (
  id: string,
  request: TestAlertRequest = {},
  persist = false,
): Promise<AlertEvent> => {
  const res = await apiClient.post<AlertEvent>(`${RULES}/${id}/test`, request, {
    params: { persist },
  });
  return res.data;
};

export const fetchAlertEvents = async (
  params: {
    ruleId?: string;
    take?: number;
  } = {},
): Promise<AlertEvent[]> => {
  const res = await apiClient.get<AlertEvent[]>(EVENTS, { params });
  return res.data ?? [];
};

export const fetchAlertEvent = async (id: string): Promise<AlertEvent> => {
  const res = await apiClient.get<AlertEvent>(`${EVENTS}/${id}`);
  return res.data;
};
