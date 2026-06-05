import { apiClient } from "@/lib/api/apiClient";

export type FeeScheduleRow = {
  id: string;
  cptCode: string;
  payerCode: string;
  amount: number;
  currencyCode: string;
  effectiveFromUtc: string; // YYYY-MM-DD (DateOnly)
  effectiveUntilUtc: string | null;
};

export type UpsertFeeScheduleRequest = {
  cptCode: string;
  payerCode: string;
  amount: number;
  currencyCode: string;
  effectiveFromUtc: string;
  effectiveUntilUtc: string | null;
};

const prefix = "/api/ehr/api/v1.0/billing/fee-schedule";

export const fetchFeeSchedule = async (
  params: { cptCode?: string; payerCode?: string } = {},
): Promise<FeeScheduleRow[]> => {
  const response = await apiClient.get<FeeScheduleRow[]>(prefix, { params });
  return response.data ?? [];
};

export const createFeeScheduleRow = async (
  request: UpsertFeeScheduleRequest,
): Promise<FeeScheduleRow> => {
  const response = await apiClient.post<FeeScheduleRow>(prefix, request);
  return response.data;
};

export const reviseFeeScheduleRow = async (
  id: string,
  request: UpsertFeeScheduleRequest,
): Promise<FeeScheduleRow> => {
  const response = await apiClient.put<FeeScheduleRow>(`${prefix}/${id}`, request);
  return response.data;
};

export const deleteFeeScheduleRow = async (id: string): Promise<void> => {
  await apiClient.delete(`${prefix}/${id}`);
};
