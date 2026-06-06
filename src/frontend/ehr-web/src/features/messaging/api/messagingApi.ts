import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/portal/messages/provider";

// Demo provider id for the care-team reply author (mirrors DEMO_PROVIDERS in care-coordination).
export const DEMO_PROVIDER_ID = "11111111-1111-1111-1111-111111111111";

export type MessageThread = {
  threadId: string;
  subject: string;
  lastMessageAtUtc: string;
  lastDirection: string;
  messageCount: number;
  unreadFromCareTeam: number;
};

export type SecureMessage = {
  id: string;
  threadId: string;
  patientId: string;
  direction: string;
  subject: string;
  body: string;
  sentAtUtc: string;
  readAtUtc?: string | null;
};

/** Care-team view of a patient's message threads. */
export const fetchPatientThreads = async (patientId: string): Promise<MessageThread[]> => {
  const response = await apiClient.get<MessageThread[]>(`${prefix}/patients/${patientId}/threads`);
  return response.data ?? [];
};

/** Messages in one of a patient's threads (care-team view). */
export const fetchThreadMessages = async (
  patientId: string,
  threadId: string,
): Promise<SecureMessage[]> => {
  const response = await apiClient.get<SecureMessage[]>(
    `${prefix}/patients/${patientId}/threads/${threadId}`,
  );
  return response.data ?? [];
};

/** Care team replies to a patient on an existing thread. */
export const replyToThread = async (
  patientId: string,
  threadId: string,
  body: { providerId: string; subject: string; body: string },
): Promise<{ id: string }> => {
  const response = await apiClient.post<{ id: string }>(
    `${prefix}/patients/${patientId}/threads/${threadId}/replies`,
    body,
  );
  return response.data;
};
