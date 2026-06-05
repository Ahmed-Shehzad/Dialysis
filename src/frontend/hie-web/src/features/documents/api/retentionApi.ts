import { apiClient } from "@/lib/api/apiClient";

export type RetentionPolicyRow = {
  id: string;
  kind: string;
  retentionDays: number;
  updatedAtUtc: string;
  updatedBy: string;
};

type Envelope<T> = { data: T };

export const fetchRetentionPolicies = async (): Promise<RetentionPolicyRow[]> => {
  const response = await apiClient.get<Envelope<RetentionPolicyRow[]>>(
    "/hie/api/v1.0/documents/retention/policies",
  );
  return response.data?.data ?? [];
};

export const upsertRetentionPolicy = async (kind: string, retentionDays: number): Promise<void> => {
  await apiClient.put(`/hie/api/v1.0/documents/retention/policies/${encodeURIComponent(kind)}`, {
    retentionDays,
  });
};

export const deleteRetentionPolicy = async (kind: string): Promise<void> => {
  await apiClient.delete(`/hie/api/v1.0/documents/retention/policies/${encodeURIComponent(kind)}`);
};
