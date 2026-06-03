import { apiClient } from "@/lib/api/apiClient";

export type ErasureRequestStatus = "Pending" | "Rejected" | "Executed";

export type ErasureModuleResult = {
  moduleSlug: string;
  recordsErased: number;
  byCategory: Record<string, number>;
};

export type ErasureRequestRow = {
  id: string;
  patientId: string;
  status: ErasureRequestStatus;
  requestedBy: string;
  requestedAtUtc: string;
  reason?: string | null;
  decisionBy?: string | null;
  decisionAtUtc?: string | null;
  decisionReason?: string | null;
  executionLog: ErasureModuleResult[];
};

export const fetchPendingErasureRequests = async (): Promise<ErasureRequestRow[]> => {
  const response = await apiClient.get<ErasureRequestRow[]>(
    "/api/hie/api/v1.0/data-subject-rights/erasure/requests",
  );
  return response.data ?? [];
};

export const approveErasureRequest = async (
  requestId: string,
  decidedBy: string,
): Promise<ErasureRequestRow> => {
  const response = await apiClient.post<ErasureRequestRow>(
    `/api/hie/api/v1.0/data-subject-rights/erasure/${requestId}/approve`,
    { decidedBy },
  );
  return response.data;
};

export const rejectErasureRequest = async (
  requestId: string,
  decidedBy: string,
  reason: string,
): Promise<ErasureRequestRow> => {
  const response = await apiClient.post<ErasureRequestRow>(
    `/api/hie/api/v1.0/data-subject-rights/erasure/${requestId}/reject`,
    { decidedBy, reason },
  );
  return response.data;
};
