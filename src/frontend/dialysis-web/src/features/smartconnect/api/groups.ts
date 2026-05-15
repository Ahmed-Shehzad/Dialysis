import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type FlowGroup } from "./types";

const path = (id?: string) =>
  id ? `${ADMIN_PREFIX}/groups/${id}` : `${ADMIN_PREFIX}/groups`;

export const fetchGroups = async (): Promise<FlowGroup[]> => {
  const res = await apiClient.get<FlowGroup[]>(path());
  return res.data ?? [];
};

export const createGroup = async (group: FlowGroup): Promise<FlowGroup> => {
  const res = await apiClient.post<FlowGroup>(path(), group);
  return res.data;
};

export const deleteGroup = async (id: string): Promise<void> => {
  await apiClient.delete(path(id));
};
