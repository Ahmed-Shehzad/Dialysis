import { apiClient } from "@/lib/api/apiClient";

/**
 * One MAR entry. Mirrors the `MedicationEntryDto` from the PDMS API exactly so the SPA
 * never has to reshape the wire payload before binding it to a row.
 */
export type MedicationEntry = {
  entryId: string;
  medicationCodeSystem: string;
  medicationCode: string;
  medicationDisplay: string;
  doseQuantity: number;
  doseUnit: string;
  route: string;
  occurredAtUtc: string;
  actorSub: string;
  wasAdministered: boolean;
  declineReason: string | null;
  relatedOrderId: string | null;
};

export type RecordAdministrationRequest = {
  patientId: string;
  codeSystem: string;
  code: string;
  display: string;
  doseQuantity: number;
  doseUnit: string;
  route: string;
  administeredBySub: string;
  administeredAtUtc?: string | null;
  relatedOrderId?: string | null;
};

export type RecordDeclineRequest = {
  patientId: string;
  codeSystem: string;
  code: string;
  display: string;
  doseQuantity: number;
  doseUnit: string;
  route: string;
  declinedBySub: string;
  reason: string;
  relatedOrderId?: string | null;
};

export const fetchSessionMedications = async (sessionId: string): Promise<MedicationEntry[]> => {
  const response = await apiClient.get<MedicationEntry[]>(
    `/api/pdms/api/v1.0/sessions/${sessionId}/medications`,
  );
  return response.data ?? [];
};

export const recordAdministration = async (
  sessionId: string,
  request: RecordAdministrationRequest,
): Promise<MedicationEntry> => {
  const response = await apiClient.post<MedicationEntry>(
    `/api/pdms/api/v1.0/sessions/${sessionId}/medications`,
    request,
  );
  return response.data;
};

export const recordDecline = async (
  sessionId: string,
  entryId: string,
  request: RecordDeclineRequest,
): Promise<MedicationEntry> => {
  const response = await apiClient.post<MedicationEntry>(
    `/api/pdms/api/v1.0/sessions/${sessionId}/medications/${entryId}/decline`,
    request,
  );
  return response.data;
};
