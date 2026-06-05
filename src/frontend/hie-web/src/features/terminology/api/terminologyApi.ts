import { apiClient } from "@/lib/api/apiClient";

export type AuthoredTerminologyRow = {
  id: string;
  resourceType: string;
  url: string;
  version: string;
  status: string;
  name: string;
  updatedAtUtc: string;
  updatedBy: string;
};

export type UpsertTerminologyInput = {
  resourceType: string;
  url: string;
  version: string;
  status: string;
  name: string;
  fhirJson: string;
};

type Envelope<T> = { data: T };

const BASE = "/hie/api/v1.0/terminology/resources";

export const fetchAuthoredTerminology = async (): Promise<AuthoredTerminologyRow[]> => {
  const response = await apiClient.get<Envelope<AuthoredTerminologyRow[]>>(BASE);
  return response.data?.data ?? [];
};

export const upsertTerminology = async (input: UpsertTerminologyInput): Promise<string> => {
  const response = await apiClient.post<Envelope<{ id: string }>>(BASE, input);
  return response.data?.data?.id ?? "";
};

export const setTerminologyStatus = async (id: string, status: string): Promise<void> => {
  await apiClient.post(`${BASE}/${encodeURIComponent(id)}/status`, { status });
};

export const deleteTerminology = async (id: string): Promise<void> => {
  await apiClient.delete(`${BASE}/${encodeURIComponent(id)}`);
};
