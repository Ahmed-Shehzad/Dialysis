import { apiClient } from "@/lib/api/apiClient";
import {
  ADMIN_PREFIX,
  type AuditEvent,
  type AuditEventCategoryValue,
  type AuditEventLevelValue,
} from "./types";

export type AuditEventQuery = {
  category?: AuditEventCategoryValue;
  level?: AuditEventLevelValue;
  flowId?: string;
  from?: string;
  to?: string;
  skip?: number;
  take?: number;
};

export const fetchAuditEvents = async (
  query: AuditEventQuery = {},
): Promise<AuditEvent[]> => {
  const res = await apiClient.get<AuditEvent[]>(`${ADMIN_PREFIX}/events`, {
    params: query,
  });
  return res.data ?? [];
};

export const fetchAuditEvent = async (id: string): Promise<AuditEvent> => {
  const res = await apiClient.get<AuditEvent>(`${ADMIN_PREFIX}/events/${id}`);
  return res.data;
};
